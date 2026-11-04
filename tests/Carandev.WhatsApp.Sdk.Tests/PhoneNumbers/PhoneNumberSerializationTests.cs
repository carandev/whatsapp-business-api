using System.Text.Json;
using Carandev.WhatsApp.Sdk.Models.Templates;
using Carandev.WhatsApp.Sdk.Models.PhoneNumbers;
using Carandev.WhatsApp.Sdk.Serialization;

namespace Carandev.WhatsApp.Sdk.Tests.PhoneNumbers;

/// <summary>
/// Smoke tests confirming <see cref="PhoneNumber"/>, its enums, and its request DTOs are actually
/// wired into the source-generated <see cref="WhatsAppJsonSerializerContext"/> — not merely that
/// the types compile, but that the generated context can serialize and deserialize them.
/// </summary>
public class PhoneNumberSerializationTests
{
    [Fact]
    public void PhoneNumber_RoundTrips_ThroughGeneratedSerializerContext()
    {
        var phoneNumber = new PhoneNumber
        {
            Id = "123456",
            DisplayName = "Acme Support",
            DisplayPhoneNumber = "+1 555-0100",
            QualityRating = PhoneNumberQualityRating.Green,
            VerifiedName = "Acme Corp",
            NameStatus = PhoneNumberNameStatus.Approved,
            CodeVerificationStatus = "VERIFIED",
            PlatformType = "CLOUD_API",
            Throughput = new PhoneNumberThroughput { Level = "STANDARD" },
            IsOfficialBusinessAccount = true,
            WebhookConfiguration = new PhoneNumberWebhookConfiguration { Application = "my-app" },
        };

        var json = JsonSerializer.Serialize(phoneNumber, WhatsAppJsonSerializerContext.Default.PhoneNumber);
        var deserialized = JsonSerializer.Deserialize(json, WhatsAppJsonSerializerContext.Default.PhoneNumber);

        Assert.Contains("\"quality_rating\":\"GREEN\"", json, StringComparison.Ordinal);
        Assert.Contains("\"name_status\":\"APPROVED\"", json, StringComparison.Ordinal);
        Assert.Contains("\"is_official_business_account\":true", json, StringComparison.Ordinal);

        Assert.NotNull(deserialized);
        Assert.Equal("123456", deserialized!.Id);
        Assert.Equal("Acme Support", deserialized.DisplayName);
        Assert.Equal("+1 555-0100", deserialized.DisplayPhoneNumber);
        Assert.Equal(PhoneNumberQualityRating.Green, deserialized.QualityRating);
        Assert.Equal("Acme Corp", deserialized.VerifiedName);
        Assert.Equal(PhoneNumberNameStatus.Approved, deserialized.NameStatus);
        Assert.Equal("VERIFIED", deserialized.CodeVerificationStatus);
        Assert.Equal("CLOUD_API", deserialized.PlatformType);
        Assert.Equal("STANDARD", deserialized.Throughput?.Level);
        Assert.True(deserialized.IsOfficialBusinessAccount);
        Assert.Equal("my-app", deserialized.WebhookConfiguration?.Application);
    }

    [Fact]
    public void TemplatesPageOfPhoneNumber_RoundTrips_ThroughGeneratedSerializerContext()
    {
        var page = new TemplatesPage<PhoneNumber>
        {
            Data =
            [
                new PhoneNumber { Id = "1", DisplayPhoneNumber = "+1 555-0100" },
                new PhoneNumber { Id = "2", DisplayPhoneNumber = "+1 555-0101" },
            ],
            Paging = new TemplatesPaging
            {
                Cursors = new TemplatesPagingCursors { Before = "before-cursor", After = "after-cursor" },
            },
        };

        var json = JsonSerializer.Serialize(page, WhatsAppJsonSerializerContext.Default.TemplatesPagePhoneNumber);
        var deserialized = JsonSerializer.Deserialize(json, WhatsAppJsonSerializerContext.Default.TemplatesPagePhoneNumber);

        Assert.NotNull(deserialized);
        Assert.NotNull(deserialized!.Data);
        Assert.Equal(2, deserialized.Data!.Count);
        Assert.Equal("1", deserialized.Data[0].Id);
        Assert.Equal("2", deserialized.Data[1].Id);
        Assert.Equal("before-cursor", deserialized.Paging?.Cursors?.Before);
        Assert.Equal("after-cursor", deserialized.Paging?.Cursors?.After);
    }

    [Theory]
    [InlineData(VerificationCodeMethod.Sms, "sms")]
    [InlineData(VerificationCodeMethod.Voice, "voice")]
    public void VerificationCodeMethod_RoundTrips_ToMetaLowercaseValue(VerificationCodeMethod method, string expectedJsonValue)
    {
        var json = JsonSerializer.Serialize(method, WhatsAppJsonSerializerContext.Default.VerificationCodeMethod);
        var deserialized = JsonSerializer.Deserialize(json, WhatsAppJsonSerializerContext.Default.VerificationCodeMethod);

        Assert.Equal($"\"{expectedJsonValue}\"", json);
        Assert.Equal(method, deserialized);
    }

    [Fact]
    public void RequestVerificationCodeRequest_Serializes_CodeMethodAndLanguage()
    {
        var request = new RequestVerificationCodeRequest(VerificationCodeMethod.Voice, "en_US");

        var json = JsonSerializer.Serialize(request, WhatsAppJsonSerializerContext.Default.RequestVerificationCodeRequest);

        Assert.Contains("\"code_method\":\"voice\"", json, StringComparison.Ordinal);
        Assert.Contains("\"language\":\"en_US\"", json, StringComparison.Ordinal);
    }

    [Fact]
    public void VerifyCodeRequest_Serializes_CodeOnly()
    {
        var request = new VerifyCodeRequest("123456");

        var json = JsonSerializer.Serialize(request, WhatsAppJsonSerializerContext.Default.VerifyCodeRequest);

        Assert.Equal("{\"code\":\"123456\"}", json);
    }
}
