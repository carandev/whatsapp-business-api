using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Carandev.WhatsApp.Sdk.Flows.Internal;

namespace Carandev.WhatsApp.Sdk.Flows;

/// <summary>
/// Decrypts requests to a WhatsApp Flow data endpoint and encrypts its responses. Host agnostic:
/// it works on the raw request body string and returns the response body string, so you own the
/// HTTP endpoint (ASP.NET Core, Azure Functions, a queue worker, ...).
/// </summary>
/// <remarks>
/// <para>
/// WhatsApp encrypts every request with a fresh AES key, itself encrypted with the RSA public key
/// you uploaded for the phone number. <see cref="DecryptRequest"/> recovers that AES key with your
/// private key (RSA-OAEP, SHA-256), then decrypts the body (AES-GCM, 16-byte tag). The returned
/// <see cref="FlowEndpointRequest"/> encrypts your response with the same AES key and the
/// bit-inverted IV, as Meta requires; return it as a plain-text body with HTTP 200.
/// </para>
/// <para>
/// When decryption fails, respond with HTTP 421 (<see cref="FlowEndpointDecryptionException.StatusCode"/>)
/// so the client refreshes your public key and retries. Validate the <c>X-Hub-Signature-256</c>
/// header first with <see cref="WhatsAppSignatureValidator"/>, and respond 432 when it does not
/// match. Instances are thread safe and meant to be reused.
/// </para>
/// </remarks>
public sealed class FlowEndpointCrypto : IDisposable
{
    private readonly RSA _privateKey;
    private readonly bool _ownsKey;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="FlowEndpointCrypto"/> class with a private key
    /// you own. The key is not disposed with this instance.
    /// </summary>
    /// <param name="privateKey">The RSA private key matching the public key uploaded to Meta.</param>
    /// <exception cref="ArgumentNullException"><paramref name="privateKey"/> is null.</exception>
    public FlowEndpointCrypto(RSA privateKey)
        : this(privateKey, ownsKey: false)
    {
    }

    private FlowEndpointCrypto(RSA privateKey, bool ownsKey)
    {
        ArgumentNullException.ThrowIfNull(privateKey);
        _privateKey = privateKey;
        _ownsKey = ownsKey;
    }

    /// <summary>
    /// Creates an instance from a PEM private key: unencrypted PKCS#8 or PKCS#1
    /// (<c>BEGIN PRIVATE KEY</c>, <c>BEGIN RSA PRIVATE KEY</c>), or, with
    /// <paramref name="passphrase"/>, encrypted PKCS#8 (<c>BEGIN ENCRYPTED PRIVATE KEY</c>).
    /// The key is disposed with the instance.
    /// </summary>
    /// <param name="privateKeyPem">The PEM text of the private key.</param>
    /// <param name="passphrase">The passphrase of an encrypted PKCS#8 key, or <see langword="null"/>.</param>
    /// <returns>A new <see cref="FlowEndpointCrypto"/>.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="privateKeyPem"/> is empty, has no supported key, or is a legacy encrypted
    /// PKCS#1 key (<c>Proc-Type: 4,ENCRYPTED</c>), which .NET cannot read. Convert that one with
    /// <c>openssl pkcs8 -topk8 -in private.pem -out private-pkcs8.pem</c>.
    /// </exception>
    /// <exception cref="CryptographicException">The passphrase is wrong or the key is corrupt.</exception>
    public static FlowEndpointCrypto FromPem(string privateKeyPem, string? passphrase = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(privateKeyPem);

        if (privateKeyPem.Contains("Proc-Type: 4,ENCRYPTED", StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "Legacy encrypted PKCS#1 keys (Proc-Type: 4,ENCRYPTED) are not supported by .NET. Convert the key to encrypted PKCS#8 with: openssl pkcs8 -topk8 -in private.pem -out private-pkcs8.pem",
                nameof(privateKeyPem));
        }

        var rsa = RSA.Create();
        try
        {
            if (passphrase is null)
            {
                rsa.ImportFromPem(privateKeyPem);
            }
            else
            {
                rsa.ImportFromEncryptedPem(privateKeyPem, passphrase);
            }

            return new FlowEndpointCrypto(rsa, ownsKey: true);
        }
        catch
        {
            rsa.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Decrypts the raw JSON body of a data endpoint request
    /// (<c>{"encrypted_flow_data", "encrypted_aes_key", "initial_vector"}</c>).
    /// </summary>
    /// <param name="requestBody">The exact request body as received.</param>
    /// <returns>The decrypted request, which also encrypts the response.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="requestBody"/> is null.</exception>
    /// <exception cref="FlowEndpointDecryptionException">
    /// The body is malformed, was encrypted for another key, was tampered with, or does not
    /// decrypt to a JSON object. Respond with <see cref="FlowEndpointDecryptionException.StatusCode"/> (421).
    /// </exception>
    /// <exception cref="ObjectDisposedException">This instance was disposed.</exception>
    public FlowEndpointRequest DecryptRequest(string requestBody)
    {
        ArgumentNullException.ThrowIfNull(requestBody);
        ObjectDisposedException.ThrowIf(_disposed, this);

        FlowEndpointEnvelope? envelope;
        try
        {
            envelope = JsonSerializer.Deserialize(requestBody, FlowEndpointJsonSerializerContext.Default.FlowEndpointEnvelope);
        }
        catch (JsonException ex)
        {
            throw new FlowEndpointDecryptionException("The request body is not a valid Flow endpoint request.", ex);
        }

        if (envelope?.EncryptedFlowData is null || envelope.EncryptedAesKey is null || envelope.InitialVector is null)
        {
            throw new FlowEndpointDecryptionException("The request body must contain encrypted_flow_data, encrypted_aes_key, and initial_vector.");
        }

        byte[] aesKey;
        byte[] plaintext;
        byte[] initialVector;
        try
        {
            var encryptedAesKey = Convert.FromBase64String(envelope.EncryptedAesKey);
            var encryptedFlowData = Convert.FromBase64String(envelope.EncryptedFlowData);
            initialVector = Convert.FromBase64String(envelope.InitialVector);

            aesKey = _privateKey.Decrypt(encryptedAesKey, RSAEncryptionPadding.OaepSHA256);
            plaintext = FlowAesGcm.Decrypt(aesKey, initialVector, encryptedFlowData);
        }
        catch (FormatException ex)
        {
            throw new FlowEndpointDecryptionException("A request field is not valid base64.", ex);
        }
        catch (CryptographicException ex)
        {
            throw new FlowEndpointDecryptionException("Failed to decrypt the request. Verify that the private key matches the public key uploaded to Meta.", ex);
        }

        var json = Encoding.UTF8.GetString(plaintext);
        FlowEndpointPayload? payload;
        try
        {
            payload = JsonSerializer.Deserialize(json, FlowEndpointJsonSerializerContext.Default.FlowEndpointPayload);
        }
        catch (JsonException ex)
        {
            throw new FlowEndpointDecryptionException("The decrypted request is not a valid JSON object.", ex);
        }

        if (payload is null)
        {
            throw new FlowEndpointDecryptionException("The decrypted request is not a valid JSON object.");
        }

        return new FlowEndpointRequest(json, payload, aesKey, initialVector);
    }

    /// <summary>
    /// Disposes the private key when it was created by <see cref="FromPem"/>.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (_ownsKey)
        {
            _privateKey.Dispose();
        }
    }
}

/// <summary>
/// A decrypted request to a WhatsApp Flow data endpoint. Use it to read what the Flow sent and to
/// encrypt the response with the request's AES key and inverted IV.
/// </summary>
public sealed class FlowEndpointRequest
{
    private const string HealthCheckResponseJson = """{"data":{"status":"active"}}""";
    private const string ErrorAcknowledgementJson = """{"data":{"acknowledged":true}}""";

    private readonly byte[] _aesKey;
    private readonly byte[] _responseIv;

    internal FlowEndpointRequest(string decryptedJson, FlowEndpointPayload payload, byte[] aesKey, byte[] initialVector)
    {
        DecryptedJson = decryptedJson;
        Version = payload.Version;
        Action = payload.Action;
        Screen = payload.Screen;
        Data = payload.Data;
        FlowToken = payload.FlowToken;
        FlowTokenSignature = payload.FlowTokenSignature;
        _aesKey = aesKey;
        _responseIv = new byte[initialVector.Length];
        for (var i = 0; i < initialVector.Length; i++)
        {
            _responseIv[i] = (byte)~initialVector[i];
        }
    }

    /// <summary>
    /// The decrypted request body, as JSON.
    /// </summary>
    public string DecryptedJson { get; }

    /// <summary>
    /// The data API version, e.g. <c>3.0</c>.
    /// </summary>
    public string? Version { get; }

    /// <summary>
    /// What triggered the request: <c>INIT</c> (the Flow opened with <c>data_exchange</c>),
    /// <c>BACK</c>, <c>data_exchange</c> (a screen was submitted), or <c>ping</c> (health check).
    /// See <see cref="FlowEndpointActions"/>.
    /// </summary>
    public string? Action { get; }

    /// <summary>
    /// The screen that sent the request. Absent for <c>ping</c>, and may be absent for <c>INIT</c>
    /// and <c>BACK</c>.
    /// </summary>
    public string? Screen { get; }

    /// <summary>
    /// The data the screen submitted, or the error details of an error notification.
    /// </summary>
    public JsonElement? Data { get; }

    /// <summary>
    /// The <c>flow_token</c> of the Flow message that opened the Flow. Absent for <c>ping</c>.
    /// </summary>
    public string? FlowToken { get; }

    /// <summary>
    /// A JWT that signs <see cref="FlowToken"/> with your app secret (HS256), sent only for newer
    /// Flow JSON and data API versions.
    /// </summary>
    public string? FlowTokenSignature { get; }

    /// <summary>
    /// Whether this is a health check (<c>action: "ping"</c>). Answer it with
    /// <see cref="EncryptHealthCheckResponse"/>.
    /// </summary>
    public bool IsHealthCheck => string.Equals(Action, FlowEndpointActions.Ping, StringComparison.Ordinal);

    /// <summary>
    /// Whether this request reports that your previous response was invalid (<c>data.error</c> is
    /// set). Answer it with <see cref="EncryptErrorAcknowledgement"/>.
    /// </summary>
    public bool IsErrorNotification =>
        Data is { ValueKind: JsonValueKind.Object } data && data.TryGetProperty("error", out _);

    internal ReadOnlySpan<byte> AesKey => _aesKey;

    /// <summary>
    /// Encrypts a JSON response, e.g. <c>{"screen":"NEXT","data":{...}}</c>.
    /// </summary>
    /// <param name="responseJson">The response JSON.</param>
    /// <returns>The base64 body to return with HTTP 200 (or 427 for an expired Flow token).</returns>
    /// <exception cref="ArgumentNullException"><paramref name="responseJson"/> is null.</exception>
    public string EncryptResponse(string responseJson)
    {
        ArgumentNullException.ThrowIfNull(responseJson);
        return Convert.ToBase64String(FlowAesGcm.Encrypt(_aesKey, _responseIv, Encoding.UTF8.GetBytes(responseJson)));
    }

    /// <summary>
    /// Serializes <paramref name="response"/> through <paramref name="typeInfo"/>, typically from
    /// your own source-generated <see cref="JsonSerializerContext"/>, and encrypts it.
    /// </summary>
    /// <typeparam name="T">The response type.</typeparam>
    /// <param name="response">The response.</param>
    /// <param name="typeInfo">The metadata used to serialize <typeparamref name="T"/>.</param>
    /// <returns>The base64 body to return.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="typeInfo"/> is null.</exception>
    public string EncryptResponse<T>(T response, JsonTypeInfo<T> typeInfo)
    {
        ArgumentNullException.ThrowIfNull(typeInfo);
        return Convert.ToBase64String(FlowAesGcm.Encrypt(_aesKey, _responseIv, JsonSerializer.SerializeToUtf8Bytes(response, typeInfo)));
    }

    /// <summary>
    /// Encrypts the health check answer, <c>{"data":{"status":"active"}}</c>.
    /// </summary>
    /// <returns>The base64 body to return.</returns>
    public string EncryptHealthCheckResponse() => EncryptResponse(HealthCheckResponseJson);

    /// <summary>
    /// Encrypts the answer to an error notification, <c>{"data":{"acknowledged":true}}</c>.
    /// </summary>
    /// <returns>The base64 body to return.</returns>
    public string EncryptErrorAcknowledgement() => EncryptResponse(ErrorAcknowledgementJson);
}

/// <summary>
/// The values of <see cref="FlowEndpointRequest.Action"/>.
/// </summary>
public static class FlowEndpointActions
{
    /// <summary>
    /// The Flow opened with the <c>data_exchange</c> action and needs its first screen.
    /// </summary>
    public const string Init = "INIT";

    /// <summary>
    /// The user went back to the previous screen and the Flow asked for its data again.
    /// </summary>
    public const string Back = "BACK";

    /// <summary>
    /// A screen was submitted.
    /// </summary>
    public const string DataExchange = "data_exchange";

    /// <summary>
    /// A health check.
    /// </summary>
    public const string Ping = "ping";
}

/// <summary>
/// HTTP status codes a Flow data endpoint returns besides 200.
/// </summary>
public static class FlowEndpointStatusCodes
{
    /// <summary>
    /// 421: the request could not be decrypted. The client refreshes your public key and retries.
    /// </summary>
    public const int DecryptionFailed = 421;

    /// <summary>
    /// 427: the Flow token is no longer valid. The client disables the Flow's button. The body may
    /// be an encrypted <c>{"error_msg":"..."}</c>.
    /// </summary>
    public const int FlowTokenExpired = 427;

    /// <summary>
    /// 432: the <c>X-Hub-Signature-256</c> header does not match the body.
    /// </summary>
    public const int SignatureInvalid = 432;
}

/// <summary>
/// Thrown by <see cref="FlowEndpointCrypto.DecryptRequest"/> when a request cannot be decrypted.
/// Respond with <see cref="StatusCode"/> (421).
/// </summary>
public sealed class FlowEndpointDecryptionException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FlowEndpointDecryptionException"/> class.
    /// </summary>
    public FlowEndpointDecryptionException()
        : this("Failed to decrypt the Flow endpoint request.")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FlowEndpointDecryptionException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public FlowEndpointDecryptionException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FlowEndpointDecryptionException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The underlying error.</param>
    public FlowEndpointDecryptionException(string message, Exception? innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// The HTTP status code to respond with: always 421.
    /// </summary>
    public int StatusCode => FlowEndpointStatusCodes.DecryptionFailed;
}

/// <summary>
/// Wire shape of an encrypted data endpoint request.
/// </summary>
internal sealed class FlowEndpointEnvelope
{
    [JsonPropertyName("encrypted_flow_data")]
    public string? EncryptedFlowData { get; set; }

    [JsonPropertyName("encrypted_aes_key")]
    public string? EncryptedAesKey { get; set; }

    [JsonPropertyName("initial_vector")]
    public string? InitialVector { get; set; }
}

/// <summary>
/// Wire shape of a decrypted data endpoint request.
/// </summary>
internal sealed class FlowEndpointPayload
{
    [JsonPropertyName("version")]
    public string? Version { get; set; }

    [JsonPropertyName("action")]
    public string? Action { get; set; }

    [JsonPropertyName("screen")]
    public string? Screen { get; set; }

    [JsonPropertyName("data")]
    public JsonElement? Data { get; set; }

    [JsonPropertyName("flow_token")]
    public string? FlowToken { get; set; }

    [JsonPropertyName("flow_token_signature")]
    public string? FlowTokenSignature { get; set; }
}

/// <summary>
/// Source-generated metadata for the internal data endpoint wire types. Separate from
/// <see cref="Serialization.WhatsAppJsonSerializerContext"/>, whose generated members are public.
/// </summary>
[JsonSerializable(typeof(FlowEndpointEnvelope))]
[JsonSerializable(typeof(FlowEndpointPayload))]
internal sealed partial class FlowEndpointJsonSerializerContext : JsonSerializerContext
{
}
