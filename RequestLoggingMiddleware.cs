using System.Diagnostics;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // 1. Generate a short 8-character correlation ID
        string correlationId = Guid.NewGuid().ToString("N")[..8];

        // 2. CRITICAL LOGIC: Set the header EARLY before calling downstream pipeline elements
        context.Response.Headers["X-Correlation-Id"] = correlationId;

        // 3. Log on entry (Inbound tracking)
        _logger.LogInformation("Inbound: {Method} {Path} [Correlation ID: {CorrelationId}]", 
            context.Request.Method, context.Request.Path, correlationId);

        // 4. Start timing the entire lifecycle downstream
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // 5. Pass control down to the next middleware or endpoint in line
            await _next(context);
        }
        finally
        {
            // 6. Execution has returned back up to us. Stop the clock.
            stopwatch.Stop();

            // 7. Log on exit (Outbound tracking with matching correlation ID)
            _logger.LogInformation("Outbound: Finished with {StatusCode} in {ElapsedMs}ms [Correlation ID: {CorrelationId}]", 
                context.Response.StatusCode, stopwatch.ElapsedMilliseconds, correlationId);
        }
    }
}