using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WhatsAppCloudApi.Clients;
using WhatsAppCloudApi.Extensions.DependencyInjection;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddWhatsAppCloudApi(builder.Configuration);

using var host = builder.Build();

// Registration-only smoke check: resolve a client to prove DI wiring works.
// No resource-client methods are called yet — they are added in future issues.
var messagesClient = host.Services.GetRequiredService<IWhatsAppMessagesClient>();

Console.WriteLine($"Resolved {messagesClient.GetType().Name} from the DI container.");
