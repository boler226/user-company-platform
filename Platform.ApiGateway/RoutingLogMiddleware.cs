using System.Diagnostics;
using Yarp.ReverseProxy.Model;

namespace Platform.ApiGateway;

public sealed class RoutingLogMiddleware(RequestDelegate next, ILogger<RoutingLogMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var timer = Stopwatch.StartNew();
        var failed = false;
        try
        {
            await next(context);
        }
        catch
        {
            failed = true;
            throw;
        }
        finally
        {
            var route = context.GetEndpoint()?.Metadata.GetMetadata<RouteModel>();
            logger.LogInformation("Gateway {Method} {Path} via {RouteId} to {ClusterId}: {StatusCode} in {ElapsedMs} ms",
                context.Request.Method, context.Request.Path, route?.Config.RouteId,
                route?.Config.ClusterId, failed ? 500 : context.Response.StatusCode, timer.ElapsedMilliseconds);
        }
    }
}
