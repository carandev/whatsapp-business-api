using System.Text.Json.Serialization;

namespace WhatsAppCloudApi.Models.Messages;

/// <summary>
/// A location message envelope sent via <c>SendLocationAsync</c>.
/// </summary>
public sealed record SendLocationMessageRequest : SendMessageRequest
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SendLocationMessageRequest"/> class.
    /// </summary>
    /// <param name="to">The recipient's phone number, in E.164 format. Must not be null, empty, or whitespace.</param>
    /// <param name="location">The location's coordinates and optional name/address.</param>
    /// <param name="context">An optional reference to a prior inbound message being replied to.</param>
    public SendLocationMessageRequest(string to, Location location, Context? context = null)
        : base(to, context)
    {
        ArgumentNullException.ThrowIfNull(location);
        Location = location;
    }

    /// <inheritdoc />
    [JsonPropertyName("type")]
    public override string Type => "location";

    /// <summary>
    /// The location's coordinates and optional name/address.
    /// </summary>
    [JsonPropertyName("location")]
    public Location Location { get; }
}

/// <summary>
/// The location content of a <see cref="SendLocationMessageRequest"/>.
/// </summary>
public sealed record Location
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Location"/> class.
    /// </summary>
    /// <param name="latitude">The location's latitude.</param>
    /// <param name="longitude">The location's longitude.</param>
    /// <param name="name">An optional name for the location.</param>
    /// <param name="address">An optional address for the location.</param>
    public Location(double latitude, double longitude, string? name = null, string? address = null)
    {
        Latitude = latitude;
        Longitude = longitude;
        Name = name;
        Address = address;
    }

    /// <summary>
    /// The location's latitude.
    /// </summary>
    [JsonPropertyName("latitude")]
    public double Latitude { get; }

    /// <summary>
    /// The location's longitude.
    /// </summary>
    [JsonPropertyName("longitude")]
    public double Longitude { get; }

    /// <summary>
    /// An optional name for the location. Omitted from the payload when <see langword="null"/>.
    /// </summary>
    [JsonPropertyName("name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Name { get; }

    /// <summary>
    /// An optional address for the location. Omitted from the payload when <see langword="null"/>.
    /// </summary>
    [JsonPropertyName("address")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Address { get; }
}
