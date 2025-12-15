using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Thingstead.Aspire.Hosting.Ngrok.Tests")]
namespace Aspire.Hosting;

internal sealed class NgrokArgumentBuilder
{
    public static string[] BuildArgs(NgrokOptions opts)
    {            
        var args = new List<string>();

        var plan = opts.Plan?.Trim().ToLowerInvariant();
        var mode = opts.Mode?.Trim().ToLowerInvariant();

        switch ((plan, mode))
        {
            case ("hobbyist", "http"):
                args.Add("http");
                args.Add($"http://{opts.TargetHostname}:{opts.TargetHostnamePort}");
                args.Add($"--hostname={opts.Hostname}");
                args.Add("--log=stdout");
                args.Add("--log-level=debug");
                break;

            default:
                args.Add("http");
                args.Add($"http://{opts.TargetHostname}:{opts.TargetHostnamePort}");
                args.Add("--log=stdout");
                break;
        }

        return [.. args];
    }
}