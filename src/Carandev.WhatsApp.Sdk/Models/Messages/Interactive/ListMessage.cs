using System.Text.Json.Serialization;

namespace Carandev.WhatsApp.Sdk.Models.Messages.Interactive;

/// <summary>
/// One selectable row within a <see cref="ListSection"/>, identified by an opaque postback
/// <see cref="Id"/> with a user-facing <see cref="Title"/> and <see cref="Description"/>.
/// Construction validates synchronously, before any HTTP request is sent: <c>id</c> must not be
/// null, empty, whitespace, or padded with leading/trailing whitespace; <c>title</c> and
/// <c>description</c> must not be null, empty, or whitespace.
/// </summary>
public sealed record ListRow
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ListRow"/> class.
    /// </summary>
    /// <param name="id">
    /// The opaque postback id returned when the recipient selects this row. Must not be null,
    /// empty, whitespace, or have leading/trailing whitespace.
    /// </param>
    /// <param name="title">The row's user-facing title. Must not be null, empty, or whitespace.</param>
    /// <param name="description">The row's user-facing description. Must not be null, empty, or whitespace.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="id"/> is null, empty, whitespace, or has leading/trailing
    /// whitespace; or when <paramref name="title"/> or <paramref name="description"/> is null,
    /// empty, or whitespace.
    /// </exception>
    public ListRow(string id, string title, string description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        if (id != id.Trim())
        {
            throw new ArgumentException("id must not have leading or trailing whitespace.", nameof(id));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        Id = id;
        Title = title;
        Description = description;
    }

    /// <summary>
    /// The opaque postback id returned when the recipient selects this row.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; }

    /// <summary>
    /// The row's user-facing title.
    /// </summary>
    [JsonPropertyName("title")]
    public string Title { get; }

    /// <summary>
    /// The row's user-facing description.
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; }
}

/// <summary>
/// A titled group of <see cref="ListRow"/> entries. Construction validates synchronously, before
/// any HTTP request is sent: <c>title</c> must not be null, empty, or whitespace; <c>rows</c> must
/// contain at least one item.
/// </summary>
public sealed record ListSection
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ListSection"/> class.
    /// </summary>
    /// <param name="title">The section's title. Must not be null, empty, or whitespace.</param>
    /// <param name="rows">The section's rows. Must contain at least one item.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="title"/> is null, empty, or whitespace; or when
    /// <paramref name="rows"/> is null or empty.
    /// </exception>
    public ListSection(string title, IReadOnlyList<ListRow> rows)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        if (rows is null || rows.Count < 1)
        {
            throw new ArgumentException("rows must contain at least 1 item.", nameof(rows));
        }

        Title = title;
        Rows = rows;
    }

    /// <summary>
    /// The section's title.
    /// </summary>
    [JsonPropertyName("title")]
    public string Title { get; }

    /// <summary>
    /// The section's rows.
    /// </summary>
    [JsonPropertyName("rows")]
    public IReadOnlyList<ListRow> Rows { get; }
}

/// <summary>
/// The <c>action</c> object of a list interactive message: the button label that opens the list,
/// plus one or more <see cref="ListSection"/> entries. Construction validates synchronously, before
/// any HTTP request is sent.
/// </summary>
public sealed record ListAction
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ListAction"/> class.
    /// </summary>
    /// <param name="button">The label of the button that opens the list. Must not be null, empty, or whitespace.</param>
    /// <param name="sections">The list's sections. Must contain at least one item.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="button"/> is null, empty, or whitespace; or when
    /// <paramref name="sections"/> is null or empty.
    /// </exception>
    public ListAction(string button, IReadOnlyList<ListSection> sections)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(button);
        if (sections is null || sections.Count < 1)
        {
            throw new ArgumentException("sections must contain at least 1 item.", nameof(sections));
        }

        Button = button;
        Sections = sections;
    }

    /// <summary>
    /// The label of the button that opens the list.
    /// </summary>
    [JsonPropertyName("button")]
    public string Button { get; }

    /// <summary>
    /// The list's sections.
    /// </summary>
    [JsonPropertyName("sections")]
    public IReadOnlyList<ListSection> Sections { get; }
}

/// <summary>
/// The <c>interactive</c> object of a list message. Not <c>[JsonPolymorphic]</c> — see remarks on
/// <see cref="SendMessageRequest"/> for the rationale shared by all interactive kinds. This type's
/// constructor accepts only <see cref="TextInteractiveHeader"/> — narrower than the header
/// hierarchy accepted by reply-buttons and CTA-URL messages — because Meta only permits text
/// headers on list messages; this makes a non-text header unrepresentable at compile time rather
/// than validated at runtime. See remarks on <see cref="Header"/> for why the property itself is
/// still declared as the wider polymorphic base.
/// </summary>
public sealed record ListInteractive
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ListInteractive"/> class.
    /// </summary>
    /// <param name="body">The required message body.</param>
    /// <param name="action">The button label and sections offered to the recipient.</param>
    /// <param name="header">An optional text-only header.</param>
    /// <param name="footer">Optional footer text.</param>
    public ListInteractive(InteractiveBody body, ListAction action, TextInteractiveHeader? header = null, InteractiveFooter? footer = null)
    {
        ArgumentNullException.ThrowIfNull(body);
        ArgumentNullException.ThrowIfNull(action);
        Body = body;
        Action = action;
        Header = header;
        Footer = footer;
    }

    /// <summary>
    /// The interactive message kind. Always <c>list</c>.
    /// </summary>
    [JsonPropertyName("type")]
    public string Type => "list";

    /// <summary>
    /// An optional text-only header. Declared as the polymorphic <see cref="InteractiveHeader"/>
    /// base — rather than the narrower <see cref="TextInteractiveHeader"/> constructor parameter
    /// type — solely so serialization resolves through <see cref="InteractiveHeader"/>'s
    /// <c>[JsonPolymorphic]</c> contract and emits the required <c>"type":"text"</c> discriminator;
    /// the constructor parameter (and <c>SendListMessageAsync</c>'s <c>headerText</c> parameter)
    /// remain the actual compile-time enforcement points that make a non-text header
    /// unrepresentable. Omitted from the payload when <see langword="null"/>.
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
    /// The button label and sections offered to the recipient.
    /// </summary>
    [JsonPropertyName("action")]
    public ListAction Action { get; }
}

/// <summary>
/// A list interactive message envelope sent via <c>SendListMessageAsync</c>.
/// </summary>
public sealed record SendListMessageRequest : SendMessageRequest
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SendListMessageRequest"/> class.
    /// </summary>
    /// <param name="to">The recipient's phone number, in E.164 format. Must not be null, empty, or whitespace.</param>
    /// <param name="bodyText">The message body text. Must not be null, empty, or whitespace.</param>
    /// <param name="buttonLabel">The label of the button that opens the list. Must not be null, empty, or whitespace.</param>
    /// <param name="sections">The list's sections. Must contain at least one item.</param>
    /// <param name="headerText">
    /// Optional header text. A plain <see cref="string"/> — not an <see cref="InteractiveHeader"/> —
    /// because Meta only permits text headers on list messages; there is no representable way to
    /// pass an image, video, or document header through this method.
    /// </param>
    /// <param name="footerText">Optional footer text.</param>
    /// <param name="context">An optional reference to a prior inbound message being replied to.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="bodyText"/>, <paramref name="buttonLabel"/> is null, empty, or
    /// whitespace; or when <paramref name="sections"/> is null or empty; or when any section's
    /// title/rows are invalid.
    /// </exception>
    public SendListMessageRequest(string to, string bodyText, string buttonLabel, IReadOnlyList<ListSection> sections, string? headerText = null, string? footerText = null, Context? context = null)
        : base(to, context)
    {
        Interactive = new ListInteractive(
            new InteractiveBody(bodyText),
            new ListAction(buttonLabel, sections),
            headerText is null ? null : new TextInteractiveHeader(headerText),
            footerText is null ? null : new InteractiveFooter(footerText));
    }

    /// <inheritdoc />
    [JsonPropertyName("type")]
    public override string Type => "interactive";

    /// <summary>
    /// The list interactive payload.
    /// </summary>
    [JsonPropertyName("interactive")]
    public ListInteractive Interactive { get; }
}
