namespace WhatsAppCloudApi.Tests.Fakes;

/// <summary>
/// A <see cref="DelegatingHandler"/> that records every request it received and returns a canned
/// <see cref="HttpResponseMessage"/> without making any real network call. By default it returns an
/// empty <c>200 OK</c>; pass a <paramref name="respond"/> factory to control the status/body per
/// test, or set <see cref="Response"/>/<see cref="ResponseFactory"/> directly.
/// </summary>
internal sealed class RecordingHandler : DelegatingHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage>? _respond;

    public RecordingHandler()
    {
    }

    public RecordingHandler(Func<HttpRequestMessage, HttpResponseMessage> respond)
    {
        _respond = respond;
    }

    /// <summary>
    /// The most recent request this handler received, or <see langword="null"/> if none yet.
    /// </summary>
    public HttpRequestMessage? LastRequest { get; private set; }

    /// <summary>
    /// The total number of requests this handler has received.
    /// </summary>
    public int RequestCount { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastRequest = request;
        RequestCount++;
        var response = _respond?.Invoke(request) ?? new HttpResponseMessage(System.Net.HttpStatusCode.OK);
        return Task.FromResult(response);
    }
}
