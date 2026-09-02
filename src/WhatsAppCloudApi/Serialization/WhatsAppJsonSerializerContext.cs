using System.Text.Json.Serialization;

namespace WhatsAppCloudApi.Serialization;

/// <summary>
/// Source-generated <see cref="JsonSerializerContext"/> for WhatsApp Cloud API DTOs. Avoids
/// reflection-based serialization so consumers can opt into trimming/AOT-friendly behavior.
/// </summary>
[JsonSerializable(typeof(WhatsAppErrorResponse))]
[JsonSerializable(typeof(WhatsAppError))]
public partial class WhatsAppJsonSerializerContext : JsonSerializerContext
{
}
