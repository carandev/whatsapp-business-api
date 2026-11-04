using System.Text.Json;
using Carandev.WhatsApp.Sdk.Models.BusinessProfile;
using Carandev.WhatsApp.Sdk.Serialization;

namespace Carandev.WhatsApp.Sdk.Tests.BusinessProfile;

/// <summary>
/// Smoke tests confirming <see cref="BusinessProfile"/>, its response envelope, and its update
/// request are actually wired into the source-generated <see cref="WhatsAppJsonSerializerContext"/>
/// — not merely that the types compile, but that the generated context can serialize and
/// deserialize them.
/// </summary>
public class BusinessProfileSerializationTests
{
    [Fact]
    public void BusinessProfile_RoundTrips_ThroughGeneratedSerializerContext()
    {
        var profile = new Carandev.WhatsApp.Sdk.Models.BusinessProfile.BusinessProfile
        {
            About = "We sell things.",
            Address = "123 Main St",
            Description = "A business.",
            Email = "hello@example.test",
            Websites = ["https://example.test"],
            Vertical = "RETAIL",
            ProfilePictureHandle = "handle-123",
        };

        var json = JsonSerializer.Serialize(profile, WhatsAppJsonSerializerContext.Default.BusinessProfile);
        var deserialized = JsonSerializer.Deserialize(json, WhatsAppJsonSerializerContext.Default.BusinessProfile);

        Assert.Contains("\"about\":\"We sell things.\"", json, StringComparison.Ordinal);
        Assert.Contains("\"vertical\":\"RETAIL\"", json, StringComparison.Ordinal);

        Assert.NotNull(deserialized);
        Assert.Equal("We sell things.", deserialized!.About);
        Assert.Equal("123 Main St", deserialized.Address);
        Assert.Equal("A business.", deserialized.Description);
        Assert.Equal("hello@example.test", deserialized.Email);
        Assert.Equal("https://example.test", deserialized.Websites?[0]);
        Assert.Equal("RETAIL", deserialized.Vertical);
        Assert.Equal("handle-123", deserialized.ProfilePictureHandle);
    }

    [Fact]
    public void BusinessProfileResponse_Unwraps_SingleElementDataArray()
    {
        var json = "{\"data\":[{\"about\":\"We sell things.\"}]}";

        var deserialized = JsonSerializer.Deserialize(json, WhatsAppJsonSerializerContext.Default.BusinessProfileResponse);

        Assert.NotNull(deserialized);
        Assert.NotNull(deserialized!.Data);
        Assert.Single(deserialized.Data!);
        Assert.Equal("We sell things.", deserialized.Data![0].About);
    }

    [Fact]
    public void UpdateBusinessProfileRequest_PartialUpdate_OmitsUnsetFields()
    {
        var request = new UpdateBusinessProfileRequest { About = "New about text." };

        var json = JsonSerializer.Serialize(request, WhatsAppJsonSerializerContext.Default.UpdateBusinessProfileRequest);

        Assert.Equal("{\"about\":\"New about text.\"}", json);
    }

    [Fact]
    public void UpdateBusinessProfileRequest_AllFieldsSet_SerializesAllSevenFields()
    {
        var request = new UpdateBusinessProfileRequest
        {
            About = "About",
            Address = "Address",
            Description = "Description",
            Email = "email@example.test",
            Websites = ["https://example.test"],
            Vertical = "RETAIL",
            ProfilePictureHandle = "handle-123",
        };

        var json = JsonSerializer.Serialize(request, WhatsAppJsonSerializerContext.Default.UpdateBusinessProfileRequest);

        Assert.Contains("\"about\":\"About\"", json, StringComparison.Ordinal);
        Assert.Contains("\"address\":\"Address\"", json, StringComparison.Ordinal);
        Assert.Contains("\"description\":\"Description\"", json, StringComparison.Ordinal);
        Assert.Contains("\"email\":\"email@example.test\"", json, StringComparison.Ordinal);
        Assert.Contains("\"websites\":[\"https://example.test\"]", json, StringComparison.Ordinal);
        Assert.Contains("\"vertical\":\"RETAIL\"", json, StringComparison.Ordinal);
        Assert.Contains("\"profile_picture_handle\":\"handle-123\"", json, StringComparison.Ordinal);
    }
}
