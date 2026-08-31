using Microsoft.Extensions.Options;
using WhatsAppBusinessApi.Api.Configuration;

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

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.Run();

// Exposed for WebApplicationFactory<Program> in integration tests.
public partial class Program;
