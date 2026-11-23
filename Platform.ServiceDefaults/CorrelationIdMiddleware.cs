using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace Platform.ServiceDefaults;

public sealed class CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
{
    public const string HeaderName = "X-Correlation-Id";

    public async Task InvokeAsync(HttpContext context)
    {
        var supplied = context.Request.Headers[HeaderName].ToString();
        var id = Guid.TryParse(supplied, out var parsed) ? parsed.ToString() : Guid.NewGuid().ToString();
        context.Request.Headers[HeaderName] = id;
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = id;
            return Task.CompletedTask;
        });
        Activity.Current?.SetTag("correlation.id", id);
        using var correlation = LogContext.PushProperty("CorrelationId", id);
        using var trace = LogContext.PushProperty("TraceId", Activity.Current?.TraceId.ToString());
        using var span = LogContext.PushProperty("SpanId", Activity.Current?.SpanId.ToString());
        var timer = Stopwatch.StartNew();
        logger.LogInformation("Request started {Method} {Path}", context.Request.Method, context.Request.Path);
        try
        {
            await next(context);
        }
        finally
        {
            logger.LogInformation("Request finished {Method} {Path} with {StatusCode} in {ElapsedMs} ms",
                context.Request.Method, context.Request.Path, context.Response.StatusCode, timer.ElapsedMilliseconds);
        }
    }
}
