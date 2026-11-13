---
title: WhatsApp Flows
description: "Create and publish Flows, send them, read the answers, and run an encrypted data endpoint."
order: 8
---

[WhatsApp Flows](https://developers.facebook.com/docs/whatsapp/flows) are forms that open inside the chat: screens with inputs, built from a Flow JSON document. The SDK covers the whole loop:

1. **Manage** Flows with `IWhatsAppFlowsClient` (create, upload Flow JSON, publish, deprecate).
2. **Send** a Flow with [`SendFlowAsync`](../sending-messages/#flow).
3. **Read** the answers from the [`nfm_reply` webhook](../webhooks/#flow-replies).
4. Optionally, serve dynamic screens from a **data endpoint** with `FlowEndpointCrypto`.

The examples assume `flows` is an `IWhatsAppFlowsClient` (registered by `AddWhatsAppCloudApi`). Flow management is WABA scoped, so set `WhatsApp:WabaId` or pass `wabaIdOverride`.

## Lifecycle

A Flow starts as a `DRAFT`. While it is a draft you can upload new Flow JSON, change its metadata, send it with `mode: FlowMode.Draft`, or delete it. Publishing makes it sendable to everyone and freezes it: a `PUBLISHED` Flow cannot be updated or deleted, only deprecated. To change one, clone it into a new draft.

```csharp
using Carandev.WhatsApp.Sdk.Models.Flows;

var created = await flows.CreateFlowAsync(
    new CreateFlowRequest("appointment_booking", [FlowCategory.AppointmentBooking])
    {
        EndpointUri = new Uri("https://api.example.com/flows/booking"), // only for Flows with an endpoint
    });

var flowId = created.Value!.Id!;

var upload = await flows.UploadFlowJsonAsync(flowId, File.ReadAllText("booking.flow.json"));
foreach (var error in upload.Value?.ValidationErrors ?? [])
{
    Console.WriteLine($"{error.LineStart}:{error.ColumnStart} {error.Message}");
}

var preview = await flows.GetFlowPreviewAsync(flowId);
Console.WriteLine(preview.Value?.PreviewUrl); // web preview, valid for 30 days

await flows.PublishFlowAsync(flowId);
```

`CreateFlowRequest` can also take the Flow JSON directly (`FlowJson`), publish in the same call (`Publish = true`, only when the JSON is valid), or copy an existing Flow (`CloneFlowId`). Meta returns validation errors with line and column positions; the JSON is stored even when it has errors, but the Flow cannot be published until they are fixed.

## Read and update

```csharp
var page = await flows.ListFlowsAsync(new FlowsListOptions { Limit = 50 });
foreach (var flow in page.Value?.Data ?? [])
{
    Console.WriteLine($"{flow.Id} {flow.Name} {flow.Status}");
}

// Meta returns id, name, status, categories, and validation_errors by default.
var details = await flows.GetFlowAsync(flowId, ["name", "status", "json_version", "endpoint_uri"]);

await flows.UpdateFlowMetadataAsync(flowId, new UpdateFlowMetadataRequest { Name = "booking_v2" });
```

Statuses and categories Meta adds later read as `FlowStatus.Unknown` and `FlowCategory.Unknown` instead of failing.

## Retire a Flow

```csharp
await flows.DeprecateFlowAsync(publishedFlowId); // published Flows: stops new sends and opens
await flows.DeleteFlowAsync(draftFlowId);        // drafts only
```

Every method returns a `Result`; Meta's errors (for example `139001`, updating a published Flow) arrive in `result.Error`. A blank Flow id or an empty update throws `ArgumentException` before any request is sent.

## Data endpoint

A Flow with an endpoint asks your server for screen data (`data_exchange`) instead of shipping it all in the Flow JSON. Every request is encrypted for your RSA key pair; [Meta's endpoint guide](https://developers.facebook.com/docs/whatsapp/flows/guides/implementingyourflowendpoint) covers generating the keys and uploading the public key for the phone number. `FlowEndpointCrypto` handles the protocol, and like the webhook processor it never touches HTTP: it takes the raw body string and gives back the response body string.

```csharp
using System.Text.Json.Nodes;
using Carandev.WhatsApp.Sdk;
using Carandev.WhatsApp.Sdk.Flows;

// Reuse one instance for concurrent requests; dispose it only on shutdown.
var crypto = FlowEndpointCrypto.FromPem(
    builder.Configuration["Flows:PrivateKeyPem"]!,
    builder.Configuration["Flows:PrivateKeyPassphrase"]);
var signature = new WhatsAppSignatureValidator(builder.Configuration["WhatsApp:AppSecret"]);

app.MapPost("/flows/booking", async (HttpRequest request) =>
{
    using var reader = new StreamReader(request.Body);
    var rawBody = await reader.ReadToEndAsync();

    if (!signature.Verify(rawBody, request.Headers["X-Hub-Signature-256"]))
    {
        return Results.StatusCode(FlowEndpointStatusCodes.SignatureInvalid); // 432
    }

    FlowEndpointRequest flow;
    try
    {
        flow = crypto.DecryptRequest(rawBody);
    }
    catch (FlowEndpointDecryptionException ex)
    {
        return Results.StatusCode(ex.StatusCode); // 421: the client refreshes the key and retries
    }

    if (flow.IsHealthCheck)
    {
        return Results.Text(flow.EncryptHealthCheckResponse());
    }

    if (flow.IsErrorNotification)
    {
        return Results.Text(flow.EncryptErrorAcknowledgement());
    }

    var next = flow.Action == FlowEndpointActions.Init
        ? new JsonObject
        {
            ["screen"] = "APPOINTMENT",
            ["data"] = new JsonObject { ["slots"] = new JsonArray("10:00", "11:00") },
        }
        : new JsonObject
        {
            ["screen"] = "SUCCESS",
            ["data"] = new JsonObject
            {
                ["extension_message_response"] = new JsonObject
                {
                    ["params"] = new JsonObject { ["flow_token"] = flow.FlowToken },
                },
            },
        };

    return Results.Text(flow.EncryptResponse(next.ToJsonString()));
});
```

`FlowEndpointRequest` exposes `Action` (`INIT`, `BACK`, `data_exchange`, `ping`; see `FlowEndpointActions`), `Screen`, `Data` as a `JsonElement`, `FlowToken`, and the raw `DecryptedJson`. `EncryptResponse<T>(value, typeInfo)` serializes through your own source-generated context. Responding with the `SUCCESS` screen and `extension_message_response.params` closes the Flow; those params, including the required `flow_token`, arrive later as the [`nfm_reply` webhook](../webhooks/#flow-replies). To reject an expired token, return `FlowEndpointStatusCodes.FlowTokenExpired` (427) with an encrypted `{"error_msg":"..."}`.

`FromPem` reads unencrypted PKCS#8 or PKCS#1 keys and, with a passphrase, encrypted PKCS#8 keys. Keys encrypted the legacy OpenSSL way (`Proc-Type: 4,ENCRYPTED`, which Meta's sample script produces) are not readable by .NET; convert them once with `openssl pkcs8 -topk8 -in private.pem -out private-pkcs8.pem`. You can also pass your own `RSA` instance (for example from a key vault) to the constructor.

AES-GCM uses a 16-byte IV here, which .NET's `AesGcm` does not accept, so the SDK builds GCM on the platform AES cipher and checks it against vectors produced by OpenSSL through Node.js, including Meta's own encryption sample.
