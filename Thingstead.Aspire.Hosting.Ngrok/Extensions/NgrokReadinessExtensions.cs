using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting
{
    internal static class NgrokReadinessExtensions
    {
        // Internal helper used by unit tests to extract the first public_url from an
        // ngrok inspection API JSON payload. Returns null when not found or parsing fails.
        internal static string? TryExtractFirstPublicUrlFromInspectionJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            try
            {
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("tunnels", out var tunnels) && tunnels.ValueKind == JsonValueKind.Array)
                {
                    foreach (var t in tunnels.EnumerateArray())
                    {
                        if (t.TryGetProperty("public_url", out var pu) && pu.ValueKind == JsonValueKind.String)
                        {
                            var url = pu.GetString();
                            if (!string.IsNullOrEmpty(url)) return url;
                        }
                    }
                }
            }
            catch { }
            return null;
        }

        // Query each candidate once and return the first public_url found (or null).
        internal static async Task<string?> QueryInspectionApiOnceAsync(
            List<Uri> candidates,
            IInspectionApiClient client,
            ILogger? logger,
            CancellationToken cancellationToken)
        {
            HttpResponseMessage? res = null;
            string triedHost = null!;
            foreach (var tu in candidates)
            {
                try
                {
                    triedHost = tu.Host;
                    logger.Info("querying inspection API at {Uri}", tu);
                    res = await client.GetAsync(tu, cancellationToken);
                    if (res.IsSuccessStatusCode) break;
                }
                catch (HttpRequestException hre)
                {
                    logger?.Warn(hre, "request to {Uri} failed", tu);
                    continue;
                }
            }

            if (res == null)
            {
                logger.Warn("no successful inspection API response from any candidate hosts");
                return null;
            }

            if (!res.IsSuccessStatusCode)
            {
                logger.Warn("inspection API returned status {Status} from host {Host}", res.StatusCode, triedHost);
            }

            var content = await res.Content.ReadAsStringAsync(cancellationToken);
            if (string.IsNullOrEmpty(content))
            {
                logger.Warn("inspection API returned empty body");
                return null;
            }

            logger.Info("inspection API response length={Length}", content.Length);
            try
            {
                return TryExtractFirstPublicUrlFromInspectionJson(content);
            }
            catch (JsonException je)
            {
                logger?.Error(je, "failed to parse inspection response as JSON");
                return null;
            }
        }

        // Polls the candidates until a public_url is found or deadline, with a small delay before starting.
        internal static async Task ProbeInspectionApiCandidatesAsync(
            NgrokResource resource,
            List<Uri> tunnelsUriCandidates,
            int pollTimeoutSeconds = 60,
            ILogger? logger = null,
            IInspectionApiClient? client = null,
            int initialDelayMs = 5000,
            CancellationToken cancellationToken = default)
        {
            // logging done directly via ILogger to ensure stable message templates

            try
            {
                client ??= new DefaultInspectionApiClient();

                if (initialDelayMs > 0) await Task.Delay(initialDelayMs, cancellationToken);

                var deadline = DateTime.UtcNow.AddSeconds(pollTimeoutSeconds);

                while (DateTime.UtcNow < deadline && !cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        var extracted = await QueryInspectionApiOnceAsync(tunnelsUriCandidates, client, logger, cancellationToken);
                        if (!string.IsNullOrEmpty(extracted))
                        {
                            logger.Info("found tunnel public_url={Url}", extracted);
                            try { resource.CompletePublicUrl(new Uri(extracted)); }
                            catch (Exception ex) { logger.Error(ex, "failed to complete public url"); }
                        }
                    }
                    catch (OperationCanceledException) { break; }
                    catch (Exception ex) { logger.Error(ex, "inspection query error"); }

                    if (!string.IsNullOrEmpty(resource.GeneratedPublicUrl?.Host)) break;
                    await Task.Delay(1000, cancellationToken);
                }
            }
            catch { /* swallow errors - best effort */ }
        }

        public static IResourceBuilder<NgrokResource> WaitForGeneratedPublicUrl(this IResourceBuilder<NgrokResource> builder, int pollTimeoutSeconds = 60, ILogger? logger = null)
        {
            ArgumentNullException.ThrowIfNull(builder);

            builder.OnResourceReady(async (r, e, c) =>
            {
                try
                {
                    var apiEndpoint = builder.GetEndpoint("http");
                    var hostsToTry = new[] { apiEndpoint.Host };
                    var tunnelsUriCandidates = hostsToTry.Select(h => new Uri($"http://{h}:{apiEndpoint.Port}/api/tunnels")).ToList();

                    await ProbeInspectionApiCandidatesAsync(r, tunnelsUriCandidates, pollTimeoutSeconds, logger, null, cancellationToken: c);
                }
                catch { /* swallow errors */ }
            });

            return builder;
        }
    }
}
