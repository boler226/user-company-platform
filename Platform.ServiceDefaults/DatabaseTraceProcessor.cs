using System.Diagnostics;
using OpenTelemetry;

namespace Platform.ServiceDefaults;

public sealed class DatabaseTraceProcessor : BaseProcessor<Activity>
{
    public override void OnEnd(Activity activity)
    {
        if (activity.Source.Name is not ("Npgsql" or "MongoDB.Driver.Core.Extensions.DiagnosticSources"))
            return;

        activity.SetTag("db.statement", null);
        activity.SetTag("db.query.text", null);
        activity.SetTag("db.connection_string", null);
        activity.SetTag("db.mongodb.command", null);
    }
}
