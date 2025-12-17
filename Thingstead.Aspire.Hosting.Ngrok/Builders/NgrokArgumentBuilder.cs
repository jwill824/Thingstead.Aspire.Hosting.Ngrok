using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Thingstead.Aspire.Hosting.Ngrok.Tests")]
namespace Aspire.Hosting;

internal sealed class NgrokArgumentBuilder
{
    public static string[] BuildArgs(NgrokOptions opts)
    {            
        var args = new List<string>();

        switch ((opts.Plan, opts.Mode))
        {
            case (NgrokPlan.Free, NgrokMode.Http):
            case (NgrokPlan.Hobbyist, NgrokMode.Http):
                args.Add("http");
                args.Add($"http://{opts.TargetHostname}:{opts.TargetHostnamePort}");
                args.Add("--log=stdout");
                args.Add("--log-level=debug");
                break;

            default:
                throw new NotImplementedException($"Ngrok plan '{opts.Plan}' with mode '{opts.Mode}' is not supported.");
        }

        return [.. args];
    }
}