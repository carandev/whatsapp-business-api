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
