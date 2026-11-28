using System.Diagnostics;
using Grpc.Core;
using Platform.Aggregator.Models;

namespace Platform.Aggregator.Clients;

public abstract class ServiceClient(ILogger logger, string service)
{
    protected async Task<ServiceResult<T>> CallAsync<T>(Func<DateTime, CancellationToken, Task<T>> call,
        CancellationToken ct) where T : class
    {
        var timer = Stopwatch.StartNew();
        var status = StatusCode.OK;
        try
        {
            return new(await call(DateTime.UtcNow.AddSeconds(4), ct), null);
        }
        catch (RpcException error)
        {
            ct.ThrowIfCancellationRequested();
            status = error.StatusCode;
            var message = status switch
            {
                StatusCode.Unavailable or StatusCode.DeadlineExceeded => $"{service} is temporarily unavailable.",
                StatusCode.NotFound => $"{service} did not find the requested data.",
                _ => $"{service} could not complete the request."
            };
            return new(null, new(service, status.ToString(), message));
        }
        finally
        {
            logger.Log(status == StatusCode.OK ? LogLevel.Information : LogLevel.Warning,
                "gRPC to {Service} returned {GrpcStatus} in {ElapsedMs} ms", service, status, timer.ElapsedMilliseconds);
        }
    }
}
