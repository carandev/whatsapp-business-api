using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace WhatsAppCloudApi.Extensions.AspNetCore;

/// <summary>
/// Compile-time proof that the <c>FrameworkReference Microsoft.AspNetCore.App</c> in this
/// project resolves ASP.NET Core hosting/HTTP types. This is scaffolding only — it is not part
/// of the public API and performs no work. The real <c>ApplicationBuilderExtensions</c> and
/// <c>UseWhatsAppWebhooks</c> middleware wiring land in a follow-up PR and will remove this file.
/// </summary>
internal static class AspNetCoreReferenceSmokeCheck
{
    /// <summary>
    /// Touches <see cref="IApplicationBuilder"/> and <see cref="HttpContext"/> so the compiler
    /// verifies both types resolve from the <c>Microsoft.AspNetCore.App</c> shared framework.
    /// </summary>
    internal static IApplicationBuilder Touch(IApplicationBuilder app, HttpContext context) => app;
}
