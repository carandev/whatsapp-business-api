namespace WhatsAppCloudApi.Tests.Fakes;

/// <summary>
/// A <see cref="DelegatingHandler"/> that records the last request it received and returns a
/// canned <see cref="HttpResponseMessage"/> without making any real network call.
/// </summary>
internal sealed class RecordingHandler : DelegatingHandler
{
    public HttpRequestMessage? LastRequest { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastRequest = request;
        return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
    }
}
