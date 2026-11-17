using System.Text.Json;
using Boyka.Application.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Boyka.API.Middleware;

public class EaApiKeyMiddleware : IMiddleware
{
    private readonly IEaRegistrationService _registrationService;

    public EaApiKeyMiddleware(IEaRegistrationService registrationService)
    {
        _registrationService = registrationService;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        // Only intercept /api/mt5/ routes
        if (!path.StartsWith("/api/mt5/", StringComparison.OrdinalIgnoreCase))
        {
            await next(context);
            return;
        }

        // Skip auth for the provisioning endpoint
        if (context.Request.Method.Equals("POST", StringComparison.OrdinalIgnoreCase)
            && path.Equals("/api/mt5/registrations", StringComparison.OrdinalIgnoreCase))
        {
            await next(context);
            return;
        }

        // Read API key from header
        if (!context.Request.Headers.TryGetValue("X-EA-Api-Key", out var keyValues)
            || string.IsNullOrWhiteSpace(keyValues))
        {
            await WriteUnauthorizedAsync(context);
            return;
        }

        var rawApiKey = keyValues.ToString();
        var registration = await _registrationService.ValidateApiKeyAsync(rawApiKey);

        if (registration is null)
        {
            await WriteUnauthorizedAsync(context);
            return;
        }

        context.Items["EaRegistration"] = registration;
        await next(context);
    }

    private static async Task WriteUnauthorizedAsync(HttpContext context)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        var body = JsonSerializer.Serialize(new { error = "Invalid or missing EA API key." });
        await context.Response.WriteAsync(body);
    }
}
