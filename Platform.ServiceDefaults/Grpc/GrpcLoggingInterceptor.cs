using System.Diagnostics;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.Logging;

namespace Platform.ServiceDefaults.Grpc;

public sealed class GrpcLoggingInterceptor(ILogger<GrpcLoggingInterceptor> logger) : Interceptor
{
    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(TRequest request,
        ServerCallContext context, UnaryServerMethod<TRequest, TResponse> next)
    {
        var timer = Stopwatch.StartNew();
        var status = StatusCode.OK;
        try
        {
            var response = await next(request, context);
            status = context.Status.StatusCode;
            return response;
        }
        catch (RpcException error)
        {
            status = error.StatusCode;
            throw;
        }
        catch (OperationCanceledException) when (context.CancellationToken.IsCancellationRequested)
        {
            status = DateTime.UtcNow >= context.Deadline ? StatusCode.DeadlineExceeded : StatusCode.Cancelled;
            throw new RpcException(new Status(status, "Call ended before completion."));
        }
        catch (Exception error)
        {
            status = StatusCode.Internal;
            logger.LogError(error, "Unexpected failure in gRPC {Method}", context.Method);
            throw new RpcException(new Status(status, "The service could not complete the request."));
        }
        finally
        {
            Activity.Current?.SetTag("rpc.grpc.status_code", (int)status);
            logger.Log(status == StatusCode.OK ? LogLevel.Information : LogLevel.Warning,
                "gRPC {Method} from {Peer} returned {GrpcStatus} in {ElapsedMs} ms",
                context.Method, context.Peer, status, timer.ElapsedMilliseconds);
        }
    }
}
