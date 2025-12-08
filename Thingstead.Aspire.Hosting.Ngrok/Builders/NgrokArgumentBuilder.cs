using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

[assembly: InternalsVisibleTo("Thingstead.Aspire.Hosting.Ngrok.Tests")]
namespace Aspire.Hosting
{
    internal sealed class NgrokArgumentBuilder
    {
        public static string[] BuildArgs(NgrokOptions opts, ILogger? logger)
        {            
            logger.Info("selected plan='{Plan}', hostname='{Host}', mode='{Mode}'", opts.Plan, opts.Domain ?? "(none)", opts.Mode);

            var args = new List<string>();

            var plan = opts.Plan?.Trim().ToLowerInvariant();
            var mode = opts.Mode?.Trim().ToLowerInvariant();

            switch ((plan, mode))
            {
                case ("hobbyist", "http"):
                    logger.Info("binding reserved hostname for hobbyist/http mode: {Hostname}", opts.Hostname);
                    args.Add("http");
                    args.Add($"http://{opts.TargetHostname}:{opts.TargetPort}");
                    args.Add($"--hostname={opts.Hostname}");
                    args.Add("--log=stdout");
                    args.Add("--log-level=debug");
                    break;

                default:
                    logger.Warn("unknown mode '{Mode}', falling back to http", opts.Mode);
                    args.Add("http");
                    args.Add($"http://{opts.TargetHostname}:{opts.TargetPort}");
                    args.Add("--log=stdout");
                    break;
            }

            return [.. args];
        }
    }
}
