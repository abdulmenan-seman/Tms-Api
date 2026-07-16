using Microsoft.AspNetCore.Http;

namespace TmsApi.Api.Middleware;

public class V1DeprecationMiddleware(RequestDelegate next)
{
    private static readonly DateTimeOffset SunsetDate = new(2026, 12, 31, 0, 0, 0, TimeSpan.Zero); // Target sunset date

    public async Task InvokeAsync(HttpContext context)
    {
        // Intercept headers at the latest possible point right before flush
        context.Response.OnStarting(() =>
        {
            if (context.Request.Path.StartsWithSegments("/api/v1")) // Only target V1 channels
            {
                context.Response.Headers["Deprecation"] = "true";
                context.Response.Headers["Sunset"] = SunsetDate.ToString("R"); // HTTP RFC date string format
                context.Response.Headers["Link"] = 
                    $"<{context.Request.Scheme}://{context.Request.Host}/api/v2{context.Request.Path.Value?[7..]}>; rel=\"successor-version\"";
            }
            return Task.CompletedTask;
        });

        await next(context);
    }
}