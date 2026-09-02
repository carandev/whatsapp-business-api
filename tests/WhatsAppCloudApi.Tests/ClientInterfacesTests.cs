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
        var resourceInterfaces = ExpectedInterfaces.Where(type => type != typeof(IWhatsAppClient));

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

        Assert.Equal(new[] { nameof(IWhatsAppClient.CreateRequest), nameof(IWhatsAppClient.SendAsync) }, methodNames);
    }
}
