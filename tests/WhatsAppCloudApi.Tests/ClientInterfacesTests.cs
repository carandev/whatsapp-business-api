using System.Reflection;
using WhatsAppCloudApi.Clients;

namespace WhatsAppCloudApi.Tests;

public class ClientInterfacesTests
{
    private static readonly Type[] ExpectedInterfaces =
    [
        typeof(IWhatsAppClient),
        typeof(IWhatsAppMessagesClient),
        typeof(IWhatsAppMediaClient),
        typeof(IWhatsAppTemplatesClient),
        typeof(IWhatsAppPhoneNumbersClient),
        typeof(IWhatsAppBusinessProfileClient),
        typeof(IWhatsAppWebhooksClient),
    ];

    [Fact]
    public void SevenClientInterfaces_ArePublic()
    {
        Assert.Equal(7, ExpectedInterfaces.Length);
        Assert.All(ExpectedInterfaces, type =>
        {
            Assert.True(type.IsInterface);
            Assert.True(type.IsPublic);
        });
    }

    [Fact]
    public void ResourceClientInterfaces_DeclareOnlyZeroOrDocumentedPlaceholderMembers()
    {
        var resourceInterfaces = ExpectedInterfaces.Where(type =>
            type != typeof(IWhatsAppClient) &&
            type != typeof(IWhatsAppTemplatesClient) &&
            type != typeof(IWhatsAppMessagesClient) &&
            type != typeof(IWhatsAppMediaClient));

        Assert.All(resourceInterfaces, type =>
        {
            var members = type.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            Assert.Empty(members);
        });
    }

    [Fact]
    public void IWhatsAppClient_DeclaresOnlyItsDocumentedContractMembers()
    {
        var methodNames = typeof(IWhatsAppClient)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Select(m => m.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            new[]
            {
                nameof(IWhatsAppClient.CreateAbsoluteRequest),
                nameof(IWhatsAppClient.CreateRequest),
                nameof(IWhatsAppClient.CreateWabaRequest),
                nameof(IWhatsAppClient.SendAsync),
            },
            methodNames);
    }

    [Fact]
    public void IWhatsAppMediaClient_DeclaresOnlyItsDocumentedMembers()
    {
        var methodNames = typeof(IWhatsAppMediaClient)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Select(m => m.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            new[]
            {
                nameof(IWhatsAppMediaClient.DownloadAsync),
                nameof(IWhatsAppMediaClient.DownloadBytesAsync),
                nameof(IWhatsAppMediaClient.GetMediaUrlAsync),
                nameof(IWhatsAppMediaClient.UploadAsync),
                nameof(IWhatsAppMediaClient.UploadAsync),
                nameof(IWhatsAppMediaClient.UploadAsync),
            },
            methodNames);
    }

    [Fact]
    public void IWhatsAppTemplatesClient_DeclaresOnlyItsDocumentedCrudMembers()
    {
        var methodNames = typeof(IWhatsAppTemplatesClient)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Select(m => m.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            new[]
            {
                nameof(IWhatsAppTemplatesClient.CreateAsync),
                nameof(IWhatsAppTemplatesClient.DeleteAsync),
                nameof(IWhatsAppTemplatesClient.GetAsync),
                nameof(IWhatsAppTemplatesClient.ListAsync),
                nameof(IWhatsAppTemplatesClient.UpdateAsync),
            },
            methodNames);
    }

    [Fact]
    public void IWhatsAppMessagesClient_DeclaresOnlyItsDocumentedSendMembers()
    {
        var methodNames = typeof(IWhatsAppMessagesClient)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Select(m => m.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            new[]
            {
                nameof(IWhatsAppMessagesClient.SendAudioAsync),
                nameof(IWhatsAppMessagesClient.SendContactsAsync),
                nameof(IWhatsAppMessagesClient.SendDocumentAsync),
                nameof(IWhatsAppMessagesClient.SendImageAsync),
                nameof(IWhatsAppMessagesClient.SendLocationAsync),
                nameof(IWhatsAppMessagesClient.SendStickerAsync),
                nameof(IWhatsAppMessagesClient.SendTemplateAsync),
                nameof(IWhatsAppMessagesClient.SendTextAsync),
                nameof(IWhatsAppMessagesClient.SendVideoAsync),
            },
            methodNames);
    }
}
