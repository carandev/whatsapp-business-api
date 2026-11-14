using System.Text.Json;
using System.Text.Json.Serialization;

namespace Carandev.WhatsApp.Sdk.Models.Messages.Interactive;

/// <summary>
/// What a Flow message does when the recipient taps its call-to-action button.
/// </summary>
public enum FlowAction
{
    /// <summary>
    /// Opens the Flow on the screen given by <see cref="FlowActionPayload.Screen"/> (or the
    /// Flow's first screen), optionally with initial <see cref="FlowActionPayload.Data"/>.
    /// Sent as <c>navigate</c>; Meta's default.
    /// </summary>
    Navigate,

    /// <summary>
    /// Asks your Flow endpoint for the first screen and its data (an <c>INIT</c> request). Sent as
    /// <c>data_exchange</c>. Requires a Flow with an endpoint.
    /// </summary>
    DataExchange,
}

/// <summary>
/// Whether a Flow message opens the published Flow or its draft.
/// </summary>
public enum FlowMode
{
    /// <summary>
    /// Opens the published Flow. Sent as <c>published</c>; Meta's default.
    /// </summary>
    Published,

    /// <summary>
    /// Opens the Flow's draft, for testing before publishing. Sent as <c>draft</c>.
    /// </summary>
    Draft,
}

/// <summary>
/// The <c>action.parameters</c> object of an interactive Flow message. Construction validates
/// synchronously, before any HTTP request is sent.
/// </summary>
public sealed record FlowMessageParameters
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FlowMessageParameters"/> class. Exactly one of
    /// <paramref name="flowId"/> or <paramref name="flowName"/> must be supplied.
    /// </summary>
    /// <param name="flowCta">The call-to-action button text. Meta advises 30 characters or less, without emoji.</param>
    /// <param name="flowId">The Flow's id. Mutually exclusive with <paramref name="flowName"/>.</param>
    /// <param name="flowName">The Flow's name. Mutually exclusive with <paramref name="flowId"/>.</param>
    /// <param name="flowToken">
    /// An identifier you generate to correlate this message with the Flow's endpoint requests and
    /// its reply. Meta uses <c>unused</c> when omitted.
    /// </param>
    /// <param name="action">What the button does. Defaults to <see cref="FlowAction.Navigate"/>.</param>
    /// <param name="screen">
    /// The screen to open. Only with <see cref="FlowAction.Navigate"/>; Meta opens the first
    /// screen when omitted.
    /// </param>
    /// <param name="data">
    /// Initial data for the screen, as a non-empty JSON object. Only with
    /// <see cref="FlowAction.Navigate"/>.
    /// </param>
    /// <param name="mode">Whether to open the published Flow or its draft. Meta uses published when omitted.</param>
    /// <exception cref="ArgumentException">
    /// <paramref name="flowCta"/> is null, empty, or whitespace; not exactly one of
    /// <paramref name="flowId"/> and <paramref name="flowName"/> is set; <paramref name="screen"/>
    /// or <paramref name="data"/> is set with <see cref="FlowAction.DataExchange"/>; or
    /// <paramref name="data"/> is not a non-empty JSON object.
    /// </exception>
    public FlowMessageParameters(
        string flowCta,
        string? flowId = null,
        string? flowName = null,
        string? flowToken = null,
        FlowAction action = FlowAction.Navigate,
        string? screen = null,
        JsonElement? data = null,
        FlowMode? mode = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(flowCta);

        var hasId = !string.IsNullOrWhiteSpace(flowId);
        var hasName = !string.IsNullOrWhiteSpace(flowName);
        if (hasId == hasName)
        {
            throw new ArgumentException("Exactly one of flowId or flowName must be supplied.", hasId ? nameof(flowName) : nameof(flowId));
        }

        if (action == FlowAction.DataExchange && (screen is not null || data is not null))
        {
            throw new ArgumentException(
                "A screen or data can only be sent with the navigate action; data_exchange gets the first screen from your endpoint.",
                screen is not null ? nameof(screen) : nameof(data));
        }

        if (screen is not null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(screen);
        }

        if (data is { } value && (value.ValueKind != JsonValueKind.Object || !value.EnumerateObject().Any()))
        {
            throw new ArgumentException("Flow data must be a non-empty JSON object.", nameof(data));
        }

        FlowCta = flowCta;
        FlowId = hasId ? flowId : null;
        FlowName = hasName ? flowName : null;
        FlowToken = flowToken;
        Action = action;
        Mode = mode;
        Payload = screen is null && data is null ? null : new FlowActionPayload(screen, data?.Clone());
    }

    /// <summary>
    /// The Flow message version. Always <c>3</c>.
    /// </summary>
    [JsonPropertyName("flow_message_version")]
    public string FlowMessageVersion => "3";

    /// <summary>
    /// The Flow's id, or <see langword="null"/> when the Flow is addressed by name.
    /// </summary>
    [JsonPropertyName("flow_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? FlowId { get; }

    /// <summary>
    /// The Flow's name, or <see langword="null"/> when the Flow is addressed by id.
    /// </summary>
    [JsonPropertyName("flow_name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? FlowName { get; }

    /// <summary>
    /// The token that correlates this message with endpoint requests and the Flow's reply.
    /// </summary>
    [JsonPropertyName("flow_token")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? FlowToken { get; }

    /// <summary>
    /// The call-to-action button text.
    /// </summary>
    [JsonPropertyName("flow_cta")]
    public string FlowCta { get; }

    /// <summary>
    /// What the button does.
    /// </summary>
    [JsonIgnore]
    public FlowAction Action { get; }

    /// <summary>
    /// The wire value of <see cref="Action"/>: <c>navigate</c> or <c>data_exchange</c>.
    /// </summary>
    [JsonPropertyName("flow_action")]
    public string FlowActionValue => Action == FlowAction.DataExchange ? "data_exchange" : "navigate";

    /// <summary>
    /// The screen and data to open with <see cref="FlowAction.Navigate"/>, or
    /// <see langword="null"/> when neither was supplied.
    /// </summary>
    [JsonPropertyName("flow_action_payload")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public FlowActionPayload? Payload { get; }

    /// <summary>
    /// Whether to open the published Flow or its draft, or <see langword="null"/> for Meta's
    /// default (published).
    /// </summary>
    [JsonIgnore]
    public FlowMode? Mode { get; }

    /// <summary>
    /// The wire value of <see cref="Mode"/>: <c>draft</c>, <c>published</c>, or omitted.
    /// </summary>
    [JsonPropertyName("mode")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ModeValue => Mode switch
    {
        FlowMode.Draft => "draft",
        FlowMode.Published => "published",
        _ => null,
    };
}

/// <summary>
/// The <c>flow_action_payload</c> object of a navigate Flow message: the screen to open and its
/// initial data.
/// </summary>
public sealed record FlowActionPayload
{
    internal FlowActionPayload(string? screen, JsonElement? data)
    {
        Screen = screen;
        Data = data;
    }

    /// <summary>
    /// The screen to open, or <see langword="null"/> for the Flow's first screen.
    /// </summary>
    [JsonPropertyName("screen")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Screen { get; }

    /// <summary>
    /// The screen's initial data, a non-empty JSON object.
    /// </summary>
    [JsonPropertyName("data")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public JsonElement? Data { get; }
}

/// <summary>
/// The <c>action</c> object of an interactive Flow message.
/// </summary>
public sealed record FlowActionEnvelope
{
    internal FlowActionEnvelope(FlowMessageParameters parameters)
    {
        Parameters = parameters;
    }

    /// <summary>
    /// The action kind. Always <c>flow</c>.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name => "flow";

    /// <summary>
    /// The Flow to open and how.
    /// </summary>
    [JsonPropertyName("parameters")]
    public FlowMessageParameters Parameters { get; }
}

/// <summary>
/// The <c>interactive</c> object of a Flow message. Not <c>[JsonPolymorphic]</c> — see remarks
/// on <see cref="SendMessageRequest"/> for the rationale shared by all interactive kinds.
/// </summary>
public sealed record FlowInteractive
{
    internal FlowInteractive(InteractiveBody body, FlowActionEnvelope action, InteractiveHeader? header, InteractiveFooter? footer)
    {
        Body = body;
        Action = action;
        Header = header;
        Footer = footer;
    }

    /// <summary>
    /// The interactive message kind. Always <c>flow</c>.
    /// </summary>
    [JsonPropertyName("type")]
    public string Type => "flow";

    /// <summary>
    /// An optional text header. Omitted from the payload when <see langword="null"/>.
    /// </summary>
    [JsonPropertyName("header")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public InteractiveHeader? Header { get; }

    /// <summary>
    /// The required message body.
    /// </summary>
    [JsonPropertyName("body")]
    public InteractiveBody Body { get; }

    /// <summary>
    /// Optional footer text. Omitted from the payload when <see langword="null"/>.
    /// </summary>
    [JsonPropertyName("footer")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public InteractiveFooter? Footer { get; }

    /// <summary>
    /// The Flow to open and how.
    /// </summary>
    [JsonPropertyName("action")]
    public FlowActionEnvelope Action { get; }
}

/// <summary>
/// An interactive Flow message envelope sent via <c>SendFlowAsync</c>.
/// </summary>
public sealed record SendFlowMessageRequest : SendMessageRequest
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SendFlowMessageRequest"/> class.
    /// </summary>
    /// <param name="to">The recipient's phone number, in E.164 format. Must not be null, empty, or whitespace.</param>
    /// <param name="bodyText">The message body text. Must not be null, empty, or whitespace.</param>
    /// <param name="flow">The Flow to open and how.</param>
    /// <param name="headerText">Optional header text.</param>
    /// <param name="footerText">Optional footer text.</param>
    /// <param name="context">An optional reference to a prior inbound message being replied to.</param>
    /// <exception cref="ArgumentException"><paramref name="bodyText"/> is null, empty, or whitespace.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="flow"/> is null.</exception>
    public SendFlowMessageRequest(string to, string bodyText, FlowMessageParameters flow, string? headerText = null, string? footerText = null, Context? context = null)
        : base(to, context)
    {
        ArgumentNullException.ThrowIfNull(flow);
        Interactive = new FlowInteractive(
            new InteractiveBody(bodyText),
            new FlowActionEnvelope(flow),
            headerText is null ? null : new TextInteractiveHeader(headerText),
            footerText is null ? null : new InteractiveFooter(footerText));
    }

    /// <inheritdoc />
    [JsonPropertyName("type")]
    public override string Type => "interactive";

    /// <summary>
    /// The Flow interactive payload.
    /// </summary>
    [JsonPropertyName("interactive")]
    public FlowInteractive Interactive { get; }
}
