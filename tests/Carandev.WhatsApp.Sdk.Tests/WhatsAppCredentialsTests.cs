using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Carandev.WhatsApp.Sdk.DependencyInjection;
using Carandev.WhatsApp.Sdk.Tests.Fakes;

namespace Carandev.WhatsApp.Sdk.Tests;

/// <summary>
/// Per-request access tokens for multi-tenant apps (issue #54): an ambient
/// <see cref="WhatsAppCredentials.UseAccessToken"/> scope wins over a registered
/// <see cref="IWhatsAppCredentialProvider"/>, which wins over <see cref="WhatsAppOptions.AccessToken"/>.
/// </summary>
public class WhatsAppCredentialsTests
{
    private const string OptionsToken = "options-token";

    private static WhatsAppOptions CreateOptions() => new()
    {
        AccessToken = OptionsToken,
        PhoneNumberId = "default-phone-id",
        ApiVersion = "v21.0",
        BaseUrl = "https://graph.facebook.com/{0}/",
    };

    private static (WhatsAppClient Client, RecordingHandler Handler) CreateClient(IWhatsAppCredentialProvider? provider = null)
    {
        var options = CreateOptions();
        var handler = new RecordingHandler();
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(string.Format(options.BaseUrl, options.ApiVersion)),
        };
        var client = provider is null
            ? new WhatsAppClient(httpClient, Options.Create(options), NullLogger<WhatsAppClient>.Instance)
            : new WhatsAppClient(httpClient, Options.Create(options), NullLogger<WhatsAppClient>.Instance, provider);
        return (client, handler);
    }

    private sealed class FixedProvider(string? token) : IWhatsAppCredentialProvider
    {
        public int Calls { get; private set; }

        public ValueTask<string?> GetAccessTokenAsync(CancellationToken cancellationToken)
        {
            Calls++;
            return ValueTask.FromResult(token);
        }
    }

    [Fact]
    public async Task SendAsync_UsesOptionsToken_WhenNoScopeOrProvider()
    {
        var (client, handler) = CreateClient();

        await client.SendAsync(client.CreateRequest(HttpMethod.Get, "{phone_number_id}"));

        Assert.Equal("Bearer", handler.LastRequest!.Headers.Authorization!.Scheme);
        Assert.Equal(OptionsToken, handler.LastRequest.Headers.Authorization.Parameter);
    }

    [Fact]
    public async Task SendAsync_UsesScopedToken_InsideUseAccessToken()
    {
        var (client, handler) = CreateClient();

        using (WhatsAppCredentials.UseAccessToken("tenant-a-token"))
        {
            await client.SendAsync(client.CreateRequest(HttpMethod.Get, "{phone_number_id}"));
        }

        Assert.Equal("Bearer", handler.LastRequest!.Headers.Authorization!.Scheme);
        Assert.Equal("tenant-a-token", handler.LastRequest.Headers.Authorization.Parameter);
    }

    [Fact]
    public async Task UseAccessToken_RestoresPreviousToken_OnDispose()
    {
        var (client, handler) = CreateClient();

        using (WhatsAppCredentials.UseAccessToken("outer-token"))
        {
            using (WhatsAppCredentials.UseAccessToken("inner-token"))
            {
                await client.SendAsync(client.CreateRequest(HttpMethod.Get, "{phone_number_id}"));
                Assert.Equal("inner-token", handler.LastRequest!.Headers.Authorization!.Parameter);
            }

            await client.SendAsync(client.CreateRequest(HttpMethod.Get, "{phone_number_id}"));
            Assert.Equal("outer-token", handler.LastRequest!.Headers.Authorization!.Parameter);
        }

        await client.SendAsync(client.CreateRequest(HttpMethod.Get, "{phone_number_id}"));
        Assert.Equal(OptionsToken, handler.LastRequest!.Headers.Authorization!.Parameter);
    }

    [Fact]
    public async Task UseAccessToken_DisposingTwice_DoesNotClobberAnOuterScope()
    {
        var (client, handler) = CreateClient();

        using (WhatsAppCredentials.UseAccessToken("outer-token"))
        {
            var inner = WhatsAppCredentials.UseAccessToken("inner-token");
            inner.Dispose();
            inner.Dispose();

            await client.SendAsync(client.CreateRequest(HttpMethod.Get, "{phone_number_id}"));
            Assert.Equal("outer-token", handler.LastRequest!.Headers.Authorization!.Parameter);
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void UseAccessToken_Throws_ForMissingToken(string? token)
    {
        Assert.ThrowsAny<ArgumentException>(() => WhatsAppCredentials.UseAccessToken(token!));
    }

    [Fact]
    public async Task UseAccessToken_IsolatesConcurrentFlows()
    {
        var (client, _) = CreateClient();
        var seen = new System.Collections.Concurrent.ConcurrentDictionary<string, string?>();
        var gate = new TaskCompletionSource();

        async Task RunAsync(string tenant)
        {
            using (WhatsAppCredentials.UseAccessToken(tenant))
            {
                await gate.Task;
                var request = client.CreateRequest(HttpMethod.Get, "{phone_number_id}");
                await client.SendAsync(request);
                seen[tenant] = request.Headers.Authorization!.Parameter;
            }
        }

        var a = Task.Run(() => RunAsync("tenant-a"));
        var b = Task.Run(() => RunAsync("tenant-b"));
        gate.SetResult();
        await Task.WhenAll(a, b);

        Assert.Equal("tenant-a", seen["tenant-a"]);
        Assert.Equal("tenant-b", seen["tenant-b"]);
    }

    [Fact]
    public async Task SendAsync_UsesProviderToken_WhenNoScope()
    {
        var provider = new FixedProvider("provider-token");
        var (client, handler) = CreateClient(provider);

        await client.SendAsync(client.CreateRequest(HttpMethod.Get, "{phone_number_id}"));

        Assert.Equal("provider-token", handler.LastRequest!.Headers.Authorization!.Parameter);
        Assert.Equal(1, provider.Calls);
    }

    [Fact]
    public async Task SendAsync_ScopedToken_WinsOverProvider()
    {
        var provider = new FixedProvider("provider-token");
        var (client, handler) = CreateClient(provider);

        using (WhatsAppCredentials.UseAccessToken("scoped-token"))
        {
            await client.SendAsync(client.CreateRequest(HttpMethod.Get, "{phone_number_id}"));
        }

        Assert.Equal("scoped-token", handler.LastRequest!.Headers.Authorization!.Parameter);
        Assert.Equal(0, provider.Calls);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SendAsync_FallsBackToOptionsToken_WhenProviderReturnsNoToken(string? token)
    {
        var (client, handler) = CreateClient(new FixedProvider(token));

        await client.SendAsync(client.CreateRequest(HttpMethod.Get, "{phone_number_id}"));

        Assert.Equal(OptionsToken, handler.LastRequest!.Headers.Authorization!.Parameter);
    }

    [Fact]
    public async Task SendAsync_KeepsAuthorizationScheme_WhenReplacingToken()
    {
        var (client, handler) = CreateClient();
        var request = client.CreateRequest(HttpMethod.Get, "./upload:session");
        request.Headers.Authorization = new AuthenticationHeaderValue("OAuth", request.Headers.Authorization!.Parameter);

        using (WhatsAppCredentials.UseAccessToken("tenant-token"))
        {
            await client.SendAsync(request);
        }

        Assert.Equal("OAuth", handler.LastRequest!.Headers.Authorization!.Scheme);
        Assert.Equal("tenant-token", handler.LastRequest.Headers.Authorization.Parameter);
    }

    [Fact]
    public async Task SendAsync_LeavesCallerSetAuthorizationUntouched()
    {
        var provider = new FixedProvider("provider-token");
        var (client, handler) = CreateClient(provider);
        var request = client.CreateRequest(HttpMethod.Get, "{phone_number_id}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "caller-token");

        using (WhatsAppCredentials.UseAccessToken("scoped-token"))
        {
            await client.SendAsync(request);
        }

        Assert.Equal("caller-token", handler.LastRequest!.Headers.Authorization!.Parameter);
        Assert.Equal(0, provider.Calls);
    }

    [Fact]
    public async Task SendAsync_LeavesRequestWithoutAuthorizationUntouched()
    {
        var (client, handler) = CreateClient(new FixedProvider("provider-token"));
        var request = new HttpRequestMessage(HttpMethod.Get, "https://example.test/file");

        using (WhatsAppCredentials.UseAccessToken("scoped-token"))
        {
            await client.SendAsync(request);
        }

        Assert.Null(handler.LastRequest!.Headers.Authorization);
    }

    [Fact]
    public async Task AddWhatsAppCloudApi_UsesRegisteredCredentialProvider()
    {
        var handler = new RecordingHandler();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["WhatsApp:AccessToken"] = OptionsToken,
                ["WhatsApp:PhoneNumberId"] = "123456789",
            })
            .Build();
        var services = new ServiceCollection();
        services.AddWhatsAppCloudApi(configuration);
        services.AddScoped<IWhatsAppCredentialProvider>(_ => new FixedProvider("di-provider-token"));
        services.AddHttpClient(nameof(IWhatsAppClient)).ConfigurePrimaryHttpMessageHandler(() => handler);
        using var root = services.BuildServiceProvider();
        using var scope = root.CreateScope();

        var client = scope.ServiceProvider.GetRequiredService<IWhatsAppClient>();
        await client.SendAsync(client.CreateRequest(HttpMethod.Get, "{phone_number_id}"));

        Assert.Equal("di-provider-token", handler.LastRequest!.Headers.Authorization!.Parameter);
    }
}
