using System.Text.Json;
using WhatsAppCloudApi.Models.Messages;
using WhatsAppCloudApi.Serialization;

namespace WhatsAppCloudApi.Tests.Messages;

/// <summary>
/// Verifies the full Meta-documented <see cref="Contact"/> schema (addresses, emails, phones,
/// name, org, urls, birthday) round-trips through the source-generated
/// <see cref="WhatsAppJsonSerializerContext"/> — confirming Phase 6's registration of the
/// <c>Contact</c> hierarchy is correctly wired, not merely that the types compile.
/// </summary>
public class ContactSerializationTests
{
    [Fact]
    public void Contact_FullSchema_RoundTrips_ThroughGeneratedSerializerContext()
    {
        var contact = new Contact
        {
            Name = new ContactName
            {
                FormattedName = "John Smith",
                FirstName = "John",
                LastName = "Smith",
                MiddleName = "Q",
                Suffix = "Jr.",
                Prefix = "Dr.",
            },
            Addresses =
            [
                new ContactAddress
                {
                    Street = "123 Main St",
                    City = "Menlo Park",
                    State = "CA",
                    Zip = "94025",
                    Country = "United States",
                    CountryCode = "US",
                    Type = "HOME",
                },
            ],
            Emails = [new ContactEmail { Email = "john@example.test", Type = "WORK" }],
            Phones = [new ContactPhone { Phone = "+16505551234", Type = "CELL", WaId = "16505551234" }],
            Org = new ContactOrg { Company = "Acme Corp", Department = "Engineering", Title = "Developer" },
            Urls = [new ContactUrl { Url = "https://example.test", Type = "WORK" }],
            Birthday = "1990-01-01",
        };

        var json = JsonSerializer.Serialize(contact, WhatsAppJsonSerializerContext.Default.Contact);
        var deserialized = JsonSerializer.Deserialize(json, WhatsAppJsonSerializerContext.Default.Contact);

        Assert.Contains("\"formatted_name\":\"John Smith\"", json, StringComparison.Ordinal);
        Assert.Contains("\"street\":\"123 Main St\"", json, StringComparison.Ordinal);
        Assert.Contains("\"email\":\"john@example.test\"", json, StringComparison.Ordinal);
        Assert.Contains("\"phone\":", json, StringComparison.Ordinal);
        Assert.Contains("\"wa_id\":\"16505551234\"", json, StringComparison.Ordinal);
        Assert.Contains("\"company\":\"Acme Corp\"", json, StringComparison.Ordinal);
        Assert.Contains("\"url\":\"https://example.test\"", json, StringComparison.Ordinal);
        Assert.Contains("\"birthday\":\"1990-01-01\"", json, StringComparison.Ordinal);

        Assert.NotNull(deserialized);
        Assert.Equal("John Smith", deserialized!.Name?.FormattedName);
        Assert.Equal("123 Main St", deserialized.Addresses?[0].Street);
        Assert.Equal("john@example.test", deserialized.Emails?[0].Email);
        Assert.Equal("+16505551234", deserialized.Phones?[0].Phone);
        Assert.Equal("16505551234", deserialized.Phones?[0].WaId);
        Assert.Equal("Acme Corp", deserialized.Org?.Company);
        Assert.Equal("https://example.test", deserialized.Urls?[0].Url);
        Assert.Equal("1990-01-01", deserialized.Birthday);
    }

    [Fact]
    public void Contact_MinimalSchema_OmitsAllOptionalFields()
    {
        var contact = new Contact();

        var json = JsonSerializer.Serialize(contact, WhatsAppJsonSerializerContext.Default.Contact);

        Assert.DoesNotContain("addresses", json, StringComparison.Ordinal);
        Assert.DoesNotContain("emails", json, StringComparison.Ordinal);
        Assert.DoesNotContain("phones", json, StringComparison.Ordinal);
        Assert.DoesNotContain("name", json, StringComparison.Ordinal);
        Assert.DoesNotContain("org", json, StringComparison.Ordinal);
        Assert.DoesNotContain("urls", json, StringComparison.Ordinal);
        Assert.DoesNotContain("birthday", json, StringComparison.Ordinal);
    }

    [Fact]
    public void SendContactsMessageRequest_RoundTrips_ThroughGeneratedSerializerContext()
    {
        var request = new SendContactsMessageRequest(
            "16505551234",
            [new Contact { Name = new ContactName { FormattedName = "Jane Doe" } }]);

        var json = JsonSerializer.Serialize(request, WhatsAppJsonSerializerContext.Default.SendContactsMessageRequest);

        Assert.Contains("\"type\":\"contacts\"", json, StringComparison.Ordinal);
        Assert.Contains("\"messaging_product\":\"whatsapp\"", json, StringComparison.Ordinal);
        Assert.Contains("\"formatted_name\":\"Jane Doe\"", json, StringComparison.Ordinal);
        Assert.DoesNotContain("\"context\"", json, StringComparison.Ordinal);
    }
}
