using System.Text.Json;
using System.Text.Json.Serialization;

namespace Carandev.WhatsApp.Sdk.Models.Flows;

/// <summary>
/// The category of a WhatsApp Flow. A Flow has at least one.
/// </summary>
[JsonConverter(typeof(FlowCategoryJsonConverter))]
public enum FlowCategory
{
    /// <summary>
    /// A category this SDK version does not know yet. Only produced when reading; it cannot be
    /// sent to Meta.
    /// </summary>
    Unknown,

    /// <summary>
    /// <c>SIGN_UP</c>.
    /// </summary>
    SignUp,

    /// <summary>
    /// <c>SIGN_IN</c>.
    /// </summary>
    SignIn,

    /// <summary>
    /// <c>APPOINTMENT_BOOKING</c>.
    /// </summary>
    AppointmentBooking,

    /// <summary>
    /// <c>LEAD_GENERATION</c>.
    /// </summary>
    LeadGeneration,

    /// <summary>
    /// <c>CONTACT_US</c>.
    /// </summary>
    ContactUs,

    /// <summary>
    /// <c>CUSTOMER_SUPPORT</c>.
    /// </summary>
    CustomerSupport,

    /// <summary>
    /// <c>SURVEY</c>.
    /// </summary>
    Survey,

    /// <summary>
    /// <c>OTHER</c>.
    /// </summary>
    Other,
}

/// <summary>
/// The lifecycle status of a WhatsApp Flow.
/// </summary>
[JsonConverter(typeof(FlowStatusJsonConverter))]
public enum FlowStatus
{
    /// <summary>
    /// A status this SDK version does not know yet.
    /// </summary>
    Unknown,

    /// <summary>
    /// <c>DRAFT</c>: editable, and sendable only with <c>mode: draft</c>.
    /// </summary>
    Draft,

    /// <summary>
    /// <c>PUBLISHED</c>: sendable; can no longer be updated or deleted.
    /// </summary>
    Published,

    /// <summary>
    /// <c>DEPRECATED</c>: can no longer be sent or opened.
    /// </summary>
    Deprecated,

    /// <summary>
    /// <c>BLOCKED</c>: blocked by health monitoring.
    /// </summary>
    Blocked,

    /// <summary>
    /// <c>THROTTLED</c>: throttled by health monitoring.
    /// </summary>
    Throttled,
}

/// <summary>
/// Reads and writes <see cref="FlowCategory"/> as Meta's upper snake case values. Unrecognized
/// values read as <see cref="FlowCategory.Unknown"/>; writing <see cref="FlowCategory.Unknown"/>
/// throws <see cref="JsonException"/>.
/// </summary>
public sealed class FlowCategoryJsonConverter : JsonConverter<FlowCategory>
{
    /// <inheritdoc />
    public override FlowCategory Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.GetString() switch
        {
            "SIGN_UP" => FlowCategory.SignUp,
            "SIGN_IN" => FlowCategory.SignIn,
            "APPOINTMENT_BOOKING" => FlowCategory.AppointmentBooking,
            "LEAD_GENERATION" => FlowCategory.LeadGeneration,
            "CONTACT_US" => FlowCategory.ContactUs,
            "CUSTOMER_SUPPORT" => FlowCategory.CustomerSupport,
            "SURVEY" => FlowCategory.Survey,
            "OTHER" => FlowCategory.Other,
            _ => FlowCategory.Unknown,
        };

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, FlowCategory value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteStringValue(ToWireValue(value));
    }

    internal static string ToWireValue(FlowCategory value) => value switch
    {
        FlowCategory.SignUp => "SIGN_UP",
        FlowCategory.SignIn => "SIGN_IN",
        FlowCategory.AppointmentBooking => "APPOINTMENT_BOOKING",
        FlowCategory.LeadGeneration => "LEAD_GENERATION",
        FlowCategory.ContactUs => "CONTACT_US",
        FlowCategory.CustomerSupport => "CUSTOMER_SUPPORT",
        FlowCategory.Survey => "SURVEY",
        FlowCategory.Other => "OTHER",
        _ => throw new JsonException($"Flow category '{value}' cannot be sent to Meta."),
    };
}

/// <summary>
/// Reads and writes <see cref="FlowStatus"/> as Meta's upper case values. Unrecognized values read
/// as <see cref="FlowStatus.Unknown"/>.
/// </summary>
public sealed class FlowStatusJsonConverter : JsonConverter<FlowStatus>
{
    /// <inheritdoc />
    public override FlowStatus Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.GetString() switch
        {
            "DRAFT" => FlowStatus.Draft,
            "PUBLISHED" => FlowStatus.Published,
            "DEPRECATED" => FlowStatus.Deprecated,
            "BLOCKED" => FlowStatus.Blocked,
            "THROTTLED" => FlowStatus.Throttled,
            _ => FlowStatus.Unknown,
        };

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, FlowStatus value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteStringValue(value switch
        {
            FlowStatus.Draft => "DRAFT",
            FlowStatus.Published => "PUBLISHED",
            FlowStatus.Deprecated => "DEPRECATED",
            FlowStatus.Blocked => "BLOCKED",
            FlowStatus.Throttled => "THROTTLED",
            _ => "UNKNOWN",
        });
    }
}
