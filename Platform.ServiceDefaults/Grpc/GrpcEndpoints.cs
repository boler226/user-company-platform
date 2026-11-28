using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;

namespace Platform.ServiceDefaults.Grpc;

public static class GrpcEndpoints
{
    public static void ConfigureInternalGrpc(this WebApplicationBuilder builder)
    {
        if (builder.Configuration.GetValue<int?>("Grpc:Port") is not { } grpcPort)
            return;

        var restPort = builder.Configuration.GetValue<int>("Rest:Port");
        builder.WebHost.ConfigureKestrel(options =>
        {
            options.ListenLocalhost(restPort, endpoint => endpoint.Protocols = HttpProtocols.Http1);
            options.ListenLocalhost(grpcPort, endpoint => endpoint.Protocols = HttpProtocols.Http2);
        });
    }
}
