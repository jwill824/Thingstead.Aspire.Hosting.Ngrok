using System.Text.Json;

namespace Aspire.Hosting;

internal static class NgrokReadinessExtensions
{
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

    internal static async Task<string?> QueryInspectionApiOnceAsync(
        List<Uri> candidates,
        IHttpClientWrapper client,
        CancellationToken cancellationToken)
    {
        HttpResponseMessage? res = null;
        foreach (var tu in candidates)
        {
            try
            {
                string triedHost = tu.Host;
                res = await client.GetAsync(tu, cancellationToken);
                if (res.IsSuccessStatusCode) break;
            }
            catch (HttpRequestException)
            {
                continue;
            }
        }

        if (res == null)
        {
            return null;
        }

        if (!res.IsSuccessStatusCode)
        {
            // inspection API returned non-success status; ignore and continue
        }

        var content = await res.Content.ReadAsStringAsync(cancellationToken);
        if (string.IsNullOrEmpty(content))
        {
            return null;
        }
        try
        {
            return TryExtractFirstPublicUrlFromInspectionJson(content);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    internal static async Task ProbeInspectionApiCandidatesAsync(
        NgrokResource resource,
        List<Uri> tunnelsUriCandidates,
        int pollTimeoutSeconds = 60,
        IHttpClientWrapper? client = null,
        int initialDelayMs = 5000,
        CancellationToken cancellationToken = default)
    {
        try
        {
            client ??= new HttpClientWrapper();

            if (initialDelayMs > 0) await Task.Delay(initialDelayMs, cancellationToken);

            var deadline = DateTime.UtcNow.AddSeconds(pollTimeoutSeconds);

            while (DateTime.UtcNow < deadline && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var extracted = await QueryInspectionApiOnceAsync(tunnelsUriCandidates, client, cancellationToken);
                    if (!string.IsNullOrEmpty(extracted))
                    {
                        try { resource.CompletePublicUrl(new Uri(extracted)); }
                        catch { /* swallow */ }
                    }
                }
                catch (OperationCanceledException) { break; }
                catch { /* swallow errors */ }

                if (resource.Uri.IsCompletedSuccessfully && !string.IsNullOrEmpty(resource.Uri.Result?.Host)) break;
                await Task.Delay(1000, cancellationToken);
            }
        }
        catch { /* swallow errors - best effort */ }
    }

    public static IResourceBuilder<NgrokResource> WaitForGeneratedPublicUrl(this IResourceBuilder<NgrokResource> builder, int pollTimeoutSeconds = 60)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.OnResourceReady(async (r, e, c) =>
        {
            try
            {
                var apiEndpoint = builder.GetEndpoint("http");
                var hostsToTry = new[] { apiEndpoint.Host };
                var tunnelsUriCandidates = hostsToTry.Select(h => new Uri($"http://{h}:{apiEndpoint.Port}/api/tunnels")).ToList();

                await ProbeInspectionApiCandidatesAsync(r, tunnelsUriCandidates, pollTimeoutSeconds, client: null, cancellationToken: c);
            }
            catch { /* swallow errors */ }
        });

        return builder;
    }
}