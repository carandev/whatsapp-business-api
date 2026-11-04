using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using WhatsAppCloudApi;
using WhatsAppCloudApi.Clients;
using WhatsAppCloudApi.Exceptions;
using WhatsAppCloudApi.Extensions.DependencyInjection;

var builder = Host.CreateApplicationBuilder(args);

// appsettings.json (committed, placeholder values only) is loaded automatically by
// Host.CreateApplicationBuilder. Layer a gitignored appsettings.Local.json on top of it (or set
// environment variables, e.g. WhatsApp__AccessToken) to supply your REAL Meta access token, phone
// number id, and recipient number without ever committing them. See README.md for the full
// quickstart.
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

builder.Services.AddWhatsAppCloudApi(builder.Configuration);

using var host = builder.Build();

var whatsAppOptions = host.Services.GetRequiredService<IOptions<WhatsAppOptions>>().Value;
var recipientPhoneNumber = builder.Configuration["SendHelloWorld:RecipientPhoneNumber"];

if (IsUnconfigured(whatsAppOptions.AccessToken) || IsUnconfigured(whatsAppOptions.PhoneNumberId) || IsUnconfigured(recipientPhoneNumber))
{
    Console.WriteLine("Credentials are not configured — nothing was sent.");
    Console.WriteLine();
    Console.WriteLine("Set WhatsApp:AccessToken, WhatsApp:PhoneNumberId, and SendHelloWorld:RecipientPhoneNumber");
    Console.WriteLine("to real values in a gitignored samples/SendHelloWorld/appsettings.Local.json file (copy the");
    Console.WriteLine("shape of appsettings.json) or via environment variables (e.g. WhatsApp__AccessToken), then");
    Console.WriteLine("run this sample again. See README.md for the full quickstart.");
    return;
}

var messagesClient = host.Services.GetRequiredService<IWhatsAppMessagesClient>();

try
{
    var result = await messagesClient.SendTextAsync(recipientPhoneNumber!, "Hello from WhatsAppCloudApi SDK!");

    if (result.IsSuccess)
    {
        Console.WriteLine($"Message sent successfully. MessageId={result.Value!.MessageId}");
    }
    else
    {
        // Business-level failure (e.g. invalid recipient, unapproved template): the Graph API call
        // completed but was rejected. Reported via Result<T>.Failure, never an exception.
        var error = result.Error!;
        Console.WriteLine("Send failed with a business error (Result<MessageResponse>.Failure):");
        Console.WriteLine($"  Code:        {error.Code}");
        Console.WriteLine($"  Subcode:     {AsDisplay(error.ErrorSubcode)}");
        Console.WriteLine($"  Title:       {AsDisplay(error.Type)}");
        Console.WriteLine($"  Message:     {AsDisplay(error.Message)}");
        Console.WriteLine($"  UserTitle:   {AsDisplay(error.ErrorUserTitle)}");
        Console.WriteLine($"  UserMessage: {AsDisplay(error.ErrorUserMsg)}");
        Console.WriteLine($"  FbTraceId:   {AsDisplay(error.FbtraceId)}");
    }
}
catch (WhatsAppException ex)
{
    // Transport-level failure: the retry pipeline exhausted WhatsAppOptions.MaxRetries attempts
    // against a transient error (5xx, rate limit, etc.) and gave up. Reported via an exception,
    // never a Result<T>.Failure.
    Console.WriteLine("Send failed after the retry pipeline exhausted its attempts (WhatsAppException):");
    Console.WriteLine($"  Code:        {ex.Code}");
    Console.WriteLine($"  Subcode:     {AsDisplay(ex.Subcode)}");
    Console.WriteLine($"  Title:       {AsDisplay(ex.Title)}");
    Console.WriteLine($"  Message:     {ex.Message}");
    Console.WriteLine($"  UserTitle:   {AsDisplay(ex.UserTitle)}");
    Console.WriteLine($"  UserMessage: {AsDisplay(ex.UserMessage)}");
    Console.WriteLine($"  FbTraceId:   {AsDisplay(ex.FbTraceId)}");
    Console.WriteLine($"  IsTransient: {ex.IsTransient}");
}

// A value counts as "unconfigured" when it is missing/blank or still the committed placeholder
// (every placeholder in appsettings.json starts with "YOUR_"), so a fresh clone fails gracefully
// with a clear message instead of sending a doomed request or crashing unhandled.
static bool IsUnconfigured(string? value) =>
    string.IsNullOrWhiteSpace(value) || value.StartsWith("YOUR_", StringComparison.OrdinalIgnoreCase);

static string AsDisplay(object? value) => value?.ToString() is { Length: > 0 } text ? text : "(none)";
