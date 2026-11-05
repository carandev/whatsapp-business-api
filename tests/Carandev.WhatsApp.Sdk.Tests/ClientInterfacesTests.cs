using Carandev.WhatsApp.Sdk.Models.Templates;
using Carandev.WhatsApp.Sdk.Models.PhoneNumbers;
using System.Reflection;
using Carandev.WhatsApp.Sdk.Clients;

namespace Carandev.WhatsApp.Sdk.Tests;

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
            type != typeof(IWhatsAppMediaClient) &&
            type != typeof(IWhatsAppPhoneNumbersClient) &&
            type != typeof(IWhatsAppBusinessProfileClient));

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
                nameof(IWhatsAppMediaClient.UploadTemplateMediaAsync),
            },
            methodNames);
    }

    [Fact]
    public async Task IWhatsAppMediaClient_UploadTemplateMediaAsync_DefaultImplementation_ThrowsNotSupported()
    {
        IWhatsAppMediaClient client = new ThirdPartyMediaClient();

        var exception = await Assert.ThrowsAsync<NotSupportedException>(() =>
            client.UploadTemplateMediaAsync(new MemoryStream([1]), "image/jpeg", "a.jpg"));

        Assert.Contains(nameof(IWhatsAppMediaClient.UploadTemplateMediaAsync), exception.Message);
        Assert.Contains(nameof(ThirdPartyMediaClient), exception.Message);
    }

    /// <summary>
    /// A minimal third-party implementation written against the interface before
    /// <c>UploadTemplateMediaAsync</c> existed: it implements only the original members.
    /// </summary>
    [Fact]
    public async Task IWhatsAppPhoneNumbersClient_ListWithOptions_DefaultImplementation_ThrowsNotSupported()
    {
        IWhatsAppPhoneNumbersClient client = new ThirdPartyPhoneNumbersClient();

        var exception = await Assert.ThrowsAsync<NotSupportedException>(() =>
            client.ListPhoneNumbersAsync(new PhoneNumbersListOptions { Limit = 1 }));

        Assert.Contains(nameof(ThirdPartyPhoneNumbersClient), exception.Message);
    }

    /// <summary>
    /// A third-party implementation written against 0.1.0: it does not implement the options
    /// overload and must still compile.
    /// </summary>
    private sealed class ThirdPartyPhoneNumbersClient : IWhatsAppPhoneNumbersClient
    {
        public Task<Result<TemplatesPage<PhoneNumber>>> ListPhoneNumbersAsync(string? wabaIdOverride = null, CancellationToken ct = default) =>
            throw new NotImplementedException();

        public Task<Result<PhoneNumber>> GetPhoneNumberAsync(string? phoneNumberIdOverride = null, CancellationToken ct = default) =>
            throw new NotImplementedException();

        public Task<Result> RequestVerificationCodeAsync(VerificationCodeMethod method, string languageCode, string? phoneNumberIdOverride = null, CancellationToken ct = default) =>
            throw new NotImplementedException();

        public Task<Result> VerifyCodeAsync(string code, string? phoneNumberIdOverride = null, CancellationToken ct = default) =>
            throw new NotImplementedException();
    }

    private sealed class ThirdPartyMediaClient : IWhatsAppMediaClient
    {
        public Task<Result<Sdk.Models.Media.MediaUploadResponse>> UploadAsync(Stream content, string mimeType, string filename, string? phoneNumberIdOverride = null, CancellationToken ct = default) =>
            throw new NotImplementedException();

        public Task<Result<Sdk.Models.Media.MediaUploadResponse>> UploadAsync(byte[] content, string mimeType, string filename, string? phoneNumberIdOverride = null, CancellationToken ct = default) =>
            throw new NotImplementedException();

        public Task<Result<Sdk.Models.Media.MediaUploadResponse>> UploadAsync(string filePath, string mimeType, string? filename = null, string? phoneNumberIdOverride = null, CancellationToken ct = default) =>
            throw new NotImplementedException();

        public Task<Result<Sdk.Models.Media.MediaUrlResponse>> GetMediaUrlAsync(string mediaId, CancellationToken ct = default) =>
            throw new NotImplementedException();

        public Task<Result<Stream>> DownloadAsync(string url, CancellationToken ct = default) =>
            throw new NotImplementedException();

        public Task<Result<byte[]>> DownloadBytesAsync(string url, CancellationToken ct = default) =>
            throw new NotImplementedException();
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
                nameof(IWhatsAppMessagesClient.SendCtaUrlButtonAsync),
                nameof(IWhatsAppMessagesClient.SendDocumentAsync),
                nameof(IWhatsAppMessagesClient.SendImageAsync),
                nameof(IWhatsAppMessagesClient.SendListMessageAsync),
                nameof(IWhatsAppMessagesClient.SendLocationAsync),
                nameof(IWhatsAppMessagesClient.SendReplyButtonsAsync),
                nameof(IWhatsAppMessagesClient.SendStickerAsync),
                nameof(IWhatsAppMessagesClient.SendTemplateAsync),
                nameof(IWhatsAppMessagesClient.SendTextAsync),
                nameof(IWhatsAppMessagesClient.SendVideoAsync),
            },
            methodNames);
    }

    [Fact]
    public void IWhatsAppPhoneNumbersClient_DeclaresOnlyItsDocumentedMembers()
    {
        var methodNames = typeof(IWhatsAppPhoneNumbersClient)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Select(m => m.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            new[]
            {
                nameof(IWhatsAppPhoneNumbersClient.GetPhoneNumberAsync),
                nameof(IWhatsAppPhoneNumbersClient.ListPhoneNumbersAsync),
                nameof(IWhatsAppPhoneNumbersClient.ListPhoneNumbersAsync),
                nameof(IWhatsAppPhoneNumbersClient.RequestVerificationCodeAsync),
                nameof(IWhatsAppPhoneNumbersClient.VerifyCodeAsync),
            },
            methodNames);
    }

    [Fact]
    public void IWhatsAppBusinessProfileClient_DeclaresOnlyItsDocumentedMembers()
    {
        var methodNames = typeof(IWhatsAppBusinessProfileClient)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Select(m => m.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            new[]
            {
                nameof(IWhatsAppBusinessProfileClient.GetBusinessProfileAsync),
                nameof(IWhatsAppBusinessProfileClient.UpdateBusinessProfileAsync),
            },
            methodNames);
    }
}
