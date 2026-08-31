using System.Net.Http.Headers;
using Microsoft.Extensions.Options;
using WhatsAppBusinessApi.Api.Clients;
using WhatsAppBusinessApi.Api.Configuration;
using WhatsAppBusinessApi.Api.ErrorMapping;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Configuration: fail-fast validation for Graph API and webhook settings.
// See design.md "Configuration" section — ValidateOnStart() runs these
// validators during host startup, before any request is served.
builder.Services.AddSingleton<IValidateOptions<GraphApiOptions>, GraphApiOptionsValidator>();
builder.Services.AddSingleton<IValidateOptions<WebhookOptions>, WebhookOptionsValidator>();

builder.Services.AddOptions<GraphApiOptions>()
    .Bind(builder.Configuration.GetSection(GraphApiOptions.SectionName))
    .ValidateOnStart();

builder.Services.AddOptions<WebhookOptions>()
    .Bind(builder.Configuration.GetSection(WebhookOptions.SectionName))
    .ValidateOnStart();

// Error mapping: translate GraphApiException into ProblemDetails via a
// single IExceptionHandler, so every Graph API failure (incl. the 24h
// customer-service-window rejection) always becomes the same response shape.
// See design.md "Error translation" decision.
builder.Services.AddExceptionHandler<GraphApiExceptionHandler>();
builder.Services.AddProblemDetails();

// Outbound messaging: typed HttpClient wrapping Meta Graph API's
// POST /{phone-number-id}/messages. IHttpClientFactory avoids socket
// exhaustion and is mockable via a fake primary handler in tests.
// See design.md "Graph client" decision.
builder.Services.AddHttpClient<IWhatsAppClient, WhatsAppClient>((sp, client) =>
{
    var graphApiOptions = sp.GetRequiredService<IOptions<GraphApiOptions>>().Value;
    client.BaseAddress = new Uri(graphApiOptions.ApiBaseUrl.TrimEnd('/') + "/");
    client.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("Bearer", graphApiOptions.AccessToken);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseExceptionHandler();

app.UseHttpsRedirection();

app.MapMessagingEndpoints();

app.Run();

// Exposed for WebApplicationFactory<Program> in integration tests.
public partial class Program;
