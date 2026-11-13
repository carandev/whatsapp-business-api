using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Carandev.WhatsApp.Sdk.Flows;
using Carandev.WhatsApp.Sdk.Flows.Internal;

namespace Carandev.WhatsApp.Sdk.Tests.Flows;

/// <summary>
/// The Flow data endpoint's encryption, checked against vectors produced by Node.js
/// (<c>TestData/generate-flow-endpoint-vectors.mjs</c>), which mirrors the WhatsApp client and
/// Meta's own endpoint example.
/// </summary>
public class FlowEndpointCryptoTests
{
    private static readonly JsonElement Vectors = JsonDocument.Parse(
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Flows", "TestData", "flow-endpoint-vectors.json"))).RootElement.Clone();

    private static string Vector(string name) => Vectors.GetProperty(name).GetString()!;

    private static string RequestBody => Vectors.GetProperty("request").GetRawText();

    private static FlowEndpointCrypto CreateCrypto() => FlowEndpointCrypto.FromPem(Vector("privateKeyPkcs8"));

    private static string BodyWith(string field, string value)
    {
        var fields = JsonSerializer.Deserialize<Dictionary<string, string>>(RequestBody)!;
        fields[field] = value;
        return JsonSerializer.Serialize(fields);
    }

    // ---------- decrypt ----------

    [Fact]
    public void DecryptRequest_RecoversThePayload_AndTypedFields()
    {
        using var crypto = CreateCrypto();

        var request = crypto.DecryptRequest(RequestBody);

        Assert.Equal(Vector("decryptedPayload"), request.DecryptedJson);
        Assert.Equal("3.0", request.Version);
        Assert.Equal("data_exchange", request.Action);
        Assert.Equal("APPOINTMENT", request.Screen);
        Assert.Equal("booking-42", request.FlowToken);
        Assert.Equal("beauty", request.Data!.Value.GetProperty("department").GetString());
        Assert.Equal(2, request.Data.Value.GetProperty("guests").GetInt32());
        Assert.False(request.IsHealthCheck);
        Assert.False(request.IsErrorNotification);
    }

    [Fact]
    public void DecryptRequest_AcceptsAnEncryptedPkcs8Key_WithItsPassphrase()
    {
        using var crypto = FlowEndpointCrypto.FromPem(Vector("privateKeyPkcs8Encrypted"), Vector("passphrase"));

        Assert.Equal(Vector("decryptedPayload"), crypto.DecryptRequest(RequestBody).DecryptedJson);
    }

    [Fact]
    public void DecryptRequest_AcceptsACallerOwnedRsaKey_AndLeavesItUsable()
    {
        using var rsa = RSA.Create();
        rsa.ImportFromPem(Vector("privateKeyPkcs8"));

        using (var crypto = new FlowEndpointCrypto(rsa))
        {
            crypto.DecryptRequest(RequestBody);
        }

        Assert.Equal(2048, rsa.KeySize); // not disposed by FlowEndpointCrypto
    }

    [Fact]
    public void FromPem_RejectsLegacyEncryptedPkcs1Key_WithConversionHint()
    {
        var ex = Assert.Throws<ArgumentException>(() => FlowEndpointCrypto.FromPem(Vector("privateKeyPkcs1Legacy"), Vector("passphrase")));
        Assert.Contains("openssl pkcs8 -topk8", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void FromPem_RejectsWrongPassphrase()
    {
        Assert.ThrowsAny<CryptographicException>(() => FlowEndpointCrypto.FromPem(Vector("privateKeyPkcs8Encrypted"), "wrong"));
    }

    [Fact]
    public void DecryptRequest_Throws421_WhenEncryptedWithAnotherKey()
    {
        using var otherKey = RSA.Create(2048);
        using var crypto = new FlowEndpointCrypto(otherKey);

        var ex = Assert.Throws<FlowEndpointDecryptionException>(() => crypto.DecryptRequest(RequestBody));
        Assert.Equal(421, ex.StatusCode);
        Assert.Equal(FlowEndpointStatusCodes.DecryptionFailed, ex.StatusCode);
    }

    [Fact]
    public void DecryptRequest_Throws421_WhenCiphertextWasTampered()
    {
        var data = Convert.FromBase64String(Vectors.GetProperty("request").GetProperty("encrypted_flow_data").GetString()!);
        data[0] ^= 0x01;
        using var crypto = CreateCrypto();

        var ex = Assert.Throws<FlowEndpointDecryptionException>(() => crypto.DecryptRequest(BodyWith("encrypted_flow_data", Convert.ToBase64String(data))));
        Assert.IsAssignableFrom<CryptographicException>(ex.InnerException);
    }

    [Fact]
    public void DecryptRequest_Throws421_WhenTagWasTampered()
    {
        var data = Convert.FromBase64String(Vectors.GetProperty("request").GetProperty("encrypted_flow_data").GetString()!);
        data[^1] ^= 0x80;
        using var crypto = CreateCrypto();

        Assert.Throws<FlowEndpointDecryptionException>(() => crypto.DecryptRequest(BodyWith("encrypted_flow_data", Convert.ToBase64String(data))));
    }

    [Fact]
    public void DecryptRequest_Throws421_WhenIvDiffers()
    {
        using var crypto = CreateCrypto();

        Assert.Throws<FlowEndpointDecryptionException>(() => crypto.DecryptRequest(BodyWith("initial_vector", Convert.ToBase64String(new byte[16]))));
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("[]")]
    [InlineData("""{"encrypted_aes_key":"AA==","initial_vector":"AA=="}""")]
    [InlineData("""{"encrypted_flow_data":"AA==","encrypted_aes_key":"%%%","initial_vector":"AA=="}""")]
    public void DecryptRequest_Throws421_ForMalformedBodies(string body)
    {
        using var crypto = CreateCrypto();

        var ex = Assert.Throws<FlowEndpointDecryptionException>(() => crypto.DecryptRequest(body));
        Assert.Equal(421, ex.StatusCode);
    }

    [Fact]
    public void DecryptRequest_Throws421_WhenFlowDataIsShorterThanTheTag()
    {
        using var crypto = CreateCrypto();

        Assert.Throws<FlowEndpointDecryptionException>(() => crypto.DecryptRequest(BodyWith("encrypted_flow_data", Convert.ToBase64String(new byte[8]))));
    }

    [Fact]
    public void DecryptRequest_RejectsNullBody()
    {
        using var crypto = CreateCrypto();

        Assert.Throws<ArgumentNullException>(() => crypto.DecryptRequest(null!));
    }

    [Fact]
    public void DecryptRequest_ThrowsObjectDisposed_AfterDispose()
    {
        var crypto = CreateCrypto();
        crypto.Dispose();

        Assert.Throws<ObjectDisposedException>(() => crypto.DecryptRequest(RequestBody));
    }

    // ---------- encrypt ----------

    [Fact]
    public void EncryptResponse_MatchesMetasReferenceImplementation()
    {
        using var crypto = CreateCrypto();
        var request = crypto.DecryptRequest(RequestBody);

        Assert.Equal(Vector("encryptedResponse"), request.EncryptResponse(Vector("responseJson")));
    }

    [Fact]
    public void EncryptResponse_Typed_SerializesThroughTheCallersContext()
    {
        using var crypto = CreateCrypto();
        var request = crypto.DecryptRequest(RequestBody);
        var response = new NextScreen { Screen = "SUCCESS", Data = new NextScreenData { ExtensionMessageResponse = new() { Params = new() { FlowToken = "booking-42" } } } };

        Assert.Equal(Vector("encryptedResponse"), request.EncryptResponse(response, FlowEndpointTestContext.Default.NextScreen));
    }

    [Fact]
    public void EncryptResponse_UsesTheFlippedIv_SoItDecryptsOnlyWithIt()
    {
        using var crypto = CreateCrypto();
        var request = crypto.DecryptRequest(RequestBody);
        var key = request.AesKey.ToArray();
        var iv = Convert.FromBase64String(Vectors.GetProperty("request").GetProperty("initial_vector").GetString()!);
        var flipped = iv.Select(b => (byte)~b).ToArray();

        var encrypted = Convert.FromBase64String(request.EncryptResponse("""{"data":{"status":"active"}}"""));

        Assert.Equal("""{"data":{"status":"active"}}""", Encoding.UTF8.GetString(FlowAesGcm.Decrypt(key, flipped, encrypted)));
        Assert.ThrowsAny<CryptographicException>(() => FlowAesGcm.Decrypt(key, iv, encrypted));
    }

    [Fact]
    public void HealthCheckAndErrorAcknowledgement_EncryptMetasFixedBodies()
    {
        using var crypto = CreateCrypto();
        var request = crypto.DecryptRequest(RequestBody);

        Assert.Equal(request.EncryptResponse("""{"data":{"status":"active"}}"""), request.EncryptHealthCheckResponse());
        Assert.Equal(request.EncryptResponse("""{"data":{"acknowledged":true}}"""), request.EncryptErrorAcknowledgement());
    }

    [Fact]
    public void EncryptResponse_RejectsNull()
    {
        using var crypto = CreateCrypto();
        var request = crypto.DecryptRequest(RequestBody);

        Assert.Throws<ArgumentNullException>(() => request.EncryptResponse((string)null!));
    }

    // ---------- request classification ----------

    [Fact]
    public void Request_RecognizesPing_AndErrorNotification()
    {
        using var crypto = CreateCrypto();
        var key = crypto.DecryptRequest(RequestBody).AesKey.ToArray();

        var ping = crypto.DecryptRequest(EncryptRequest(key, """{"version":"3.0","action":"ping"}"""));
        Assert.True(ping.IsHealthCheck);
        Assert.Null(ping.FlowToken);
        Assert.Null(ping.Screen);
        Assert.Null(ping.Data);

        var error = crypto.DecryptRequest(EncryptRequest(key, """{"version":"3.0","flow_token":"t","action":"data_exchange","data":{"error":"INVALID_SCREEN_TRANSITION","error_message":"oops"}}"""));
        Assert.True(error.IsErrorNotification);
        Assert.False(error.IsHealthCheck);
    }

    [Fact]
    public void Request_Throws421_WhenDecryptedPayloadIsNotAJsonObject()
    {
        using var crypto = CreateCrypto();
        var key = crypto.DecryptRequest(RequestBody).AesKey.ToArray();

        Assert.Throws<FlowEndpointDecryptionException>(() => crypto.DecryptRequest(EncryptRequest(key, "not json")));
    }

    // ---------- GCM with a non-96-bit IV (the path .NET's AesGcm does not support) ----------

    [Fact]
    public void Gcm_MatchesNode_ForA60ByteIvWithAad()
    {
        var gcm = Vectors.GetProperty("gcm");
        byte[] Hex(string name) => Convert.FromHexString(gcm.GetProperty(name).GetString()!);

        var sealedBytes = FlowAesGcm.Encrypt(Hex("key"), Hex("iv"), Hex("plaintext"), Hex("aad"));

        Assert.Equal(Hex("ciphertext").Concat(Hex("tag")).ToArray(), sealedBytes);
        Assert.Equal(Hex("plaintext"), FlowAesGcm.Decrypt(Hex("key"), Hex("iv"), sealedBytes, Hex("aad")));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(15)]
    [InlineData(16)]
    [InlineData(17)]
    [InlineData(1000)]
    public void Gcm_RoundTrips_ForEveryLength(int length)
    {
        var key = RandomNumberGenerator.GetBytes(16);
        var iv = RandomNumberGenerator.GetBytes(16);
        var plaintext = RandomNumberGenerator.GetBytes(length);

        var sealedBytes = FlowAesGcm.Encrypt(key, iv, plaintext);

        Assert.Equal(length + 16, sealedBytes.Length);
        Assert.Equal(plaintext, FlowAesGcm.Decrypt(key, iv, sealedBytes));
    }

    [Fact]
    public void Gcm_With12ByteIv_MatchesDotNetAesGcm()
    {
        var key = RandomNumberGenerator.GetBytes(16);
        var nonce = RandomNumberGenerator.GetBytes(12);
        var plaintext = RandomNumberGenerator.GetBytes(77);
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[16];
        using (var aesGcm = new AesGcm(key, 16))
        {
            aesGcm.Encrypt(nonce, plaintext, ciphertext, tag);
        }

        Assert.Equal(ciphertext.Concat(tag).ToArray(), FlowAesGcm.Encrypt(key, nonce, plaintext));
    }

    private static string EncryptRequest(byte[] aesKey, string payload)
    {
        var iv = RandomNumberGenerator.GetBytes(16);
        using var rsa = RSA.Create();
        rsa.ImportFromPem(Vector("privateKeyPkcs8"));
        return JsonSerializer.Serialize(new Dictionary<string, string>
        {
            ["encrypted_flow_data"] = Convert.ToBase64String(FlowAesGcm.Encrypt(aesKey, iv, Encoding.UTF8.GetBytes(payload))),
            ["encrypted_aes_key"] = Convert.ToBase64String(rsa.Encrypt(aesKey, RSAEncryptionPadding.OaepSHA256)),
            ["initial_vector"] = Convert.ToBase64String(iv),
        });
    }
}

public sealed class NextScreen
{
    [JsonPropertyName("screen")]
    public string? Screen { get; set; }

    [JsonPropertyName("data")]
    public NextScreenData? Data { get; set; }
}

public sealed class NextScreenData
{
    [JsonPropertyName("extension_message_response")]
    public ExtensionMessageResponse? ExtensionMessageResponse { get; set; }
}

public sealed class ExtensionMessageResponse
{
    [JsonPropertyName("params")]
    public ExtensionParams? Params { get; set; }
}

public sealed class ExtensionParams
{
    [JsonPropertyName("flow_token")]
    public string? FlowToken { get; set; }
}

[JsonSerializable(typeof(NextScreen))]
internal sealed partial class FlowEndpointTestContext : JsonSerializerContext
{
}
