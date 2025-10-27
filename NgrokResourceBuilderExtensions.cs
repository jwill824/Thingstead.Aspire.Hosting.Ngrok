using System.Text.Json;
// Put extensions in the Aspire.Hosting namespace to ease discovery as referencing
// the Aspire hosting package automatically adds this namespace.
namespace Aspire.Hosting;

/// <summary>
/// Extension methods for adding and configuring an <see cref="NgrokResource"/> in an
/// <see cref="IDistributedApplicationBuilder"/>. These are placed in the <c>Aspire.Hosting</c>
/// namespace for discoverability when referencing the Aspire hosting packages.
/// </summary>
public static class NgrokResourceBuilderExtensions
{
    private const string NGROK_AUTHTOKEN = nameof(NGROK_AUTHTOKEN);

    /// <summary>
    /// Adds an <see cref="NgrokResource"/> to the provided <paramref name="builder"/> and
    /// configures the container image, inspection endpoint and environment variables.
    /// The returned <see cref="IResourceBuilder{NgrokResource}"/> can be used to further
    /// configure the resource (for example, to set commands or additional endpoints).
    /// </summary>
    /// <param name="builder">The application distributed builder.</param>
    /// <param name="name">A unique name for the resource within the builder.</param>
    /// <param name="port">Host port to bind the ngrok inspection API to (default: 4041).</param>
    /// <param name="targetPort">Container port that ngrok will expose (default: 4040).</param>
    /// <param name="authToken">A parameter resource that provides the ngrok auth token.</param>
    /// <param name="logger">Optional logger delegate used while waiting for the public URL.</param>
    /// <returns>An <see cref="IResourceBuilder{NgrokResource}"/> for further configuration.</returns>
    public static IResourceBuilder<NgrokResource> AddNgrok(
        this IDistributedApplicationBuilder builder,
        string name,
        int port = 4041,
        int targetPort = 4040,
        IResourceBuilder<ParameterResource>? authToken = null,
        Action<string>? logger = null)
    {
        var resource = new NgrokResource(name, authToken?.Resource ?? throw new ArgumentNullException(nameof(authToken)));

        var rb = builder.AddResource(resource)
                      .WithImage(NgrokContainerImageTags.Image)
                      .WithImageRegistry(NgrokContainerImageTags.Registry)
                      .WithImageTag(NgrokContainerImageTags.Tag)
                      .WithHttpEndpoint(targetPort: targetPort, port: port)
                      .WithEnvironment(context =>
                      {
                          context.EnvironmentVariables[NGROK_AUTHTOKEN] = resource.AuthTokenParameter;
                      })
                      .WaitForGeneratedPublicUrl(logger: logger);

        return rb;
    }

    private static IResourceBuilder<NgrokResource> WaitForGeneratedPublicUrl(this IResourceBuilder<NgrokResource> builder, int pollTimeoutSeconds = 60, Action<string>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        Action<string> log = logger ?? (m => Console.WriteLine(m));

        // Register a host-side readiness hook that polls the ngrok inspection API and
        // completes the resource's public URL Task when a tunnel with a public_url is found.
        builder.OnResourceReady(async (r, e, c) =>
        {
            try
            {
                using var http = new HttpClient();
                http.Timeout = TimeSpan.FromSeconds(10);

                await Task.Delay(5000, c);

                var apiEndpoint = builder.GetEndpoint("http");
                var hostsToTry = new[] { apiEndpoint.Host };
                var tunnelsUriCandidates = hostsToTry.Select(h => new Uri($"http://{h}:{apiEndpoint.Port}/api/tunnels")).ToList();

                var deadline = DateTime.UtcNow.AddSeconds(pollTimeoutSeconds);

                while (DateTime.UtcNow < deadline && !c.IsCancellationRequested)
                {
                    try
                    {
                        HttpResponseMessage? res = null;
                        string triedHost = null!;
                        foreach (var tu in tunnelsUriCandidates)
                        {
                            try
                            {
                                triedHost = tu.Host;
                                log($"[ngrok] querying inspection API at {tu}");
                                res = await http.GetAsync(tu, c);
                                if (res.IsSuccessStatusCode) break;
                                // If not success, continue to try other candidates
                            }
                            catch (HttpRequestException hre)
                            {
                                log($"[ngrok] request to {tu} failed: {hre.Message}");
                                continue;
                            }
                        }
                        if (res == null)
                        {
                            log("[ngrok] no successful inspection API response from any candidate hosts");
                        }
                        else
                        {
                            if (!res.IsSuccessStatusCode)
                            {
                                log($"[ngrok] inspection API returned status {res.StatusCode} from host {triedHost}");
                            }
                            var content = await res.Content.ReadAsStringAsync(c);
                            if (string.IsNullOrEmpty(content))
                            {
                                log("[ngrok] inspection API returned empty body");
                            }
                            else
                            {
                                log($"[ngrok] inspection API response: {content}");
                                try
                                {
                                    using var doc = JsonDocument.Parse(content);
                                    if (doc.RootElement.TryGetProperty("tunnels", out var tunnels) && tunnels.ValueKind == JsonValueKind.Array)
                                    {
                                        foreach (var t in tunnels.EnumerateArray())
                                        {
                                            if (t.TryGetProperty("public_url", out var pu) && pu.ValueKind == JsonValueKind.String)
                                            {
                                                var url = pu.GetString();
                                                if (!string.IsNullOrEmpty(url))
                                                {
                                                    log($"[ngrok] found tunnel public_url={url}");
                                                    if (url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                                                    {
                                                        r.CompletePublicUrl(new Uri(url));
                                                        break;
                                                    }
                                                    r.CompletePublicUrl(new Uri(url));
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        log("[ngrok] no 'tunnels' array found in inspection response");
                                    }
                                }
                                catch (JsonException je)
                                {
                                    log($"[ngrok] failed to parse inspection response as JSON: {je.Message}");
                                }
                            }
                        }
                    }
                    catch (OperationCanceledException) { break; }
                    catch (Exception ex) { log($"[ngrok] inspection query error: {ex.Message}"); }

                    if (!string.IsNullOrEmpty(r.GeneratedPublicUrl.Host)) break;
                    await Task.Delay(1000, c);
                }
            }
            catch { /* swallow errors - resource shouldn't crash host for this */ }
        });

        return builder;
    }

    // (WithDefaultCommand documented version appears further below.)

    /// <summary>
    /// Configure the resource to use the default ngrok command that forwards HTTP from the
    /// specified <paramref name="host"/> and <paramref name="port"/> into the container.
    /// This is a convenience method that sets the common command-line arguments for http tunnels.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="host">The host to forward to (for example <c>host.docker.internal</c>).</param>
    /// <param name="port">The port on the host to forward (default: 80).</param>
    /// <returns>The builder for chaining.</returns>
    public static IResourceBuilder<NgrokResource> WithDefaultCommand(this IResourceBuilder<NgrokResource> builder, string host, int port = 80)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.WithArgs("http", $"http://{host}:{port}", "--log=stdout");
    }
}

// This class just contains constant strings that can be updated periodically
// when new versions of the underlying container are released.
internal static class NgrokContainerImageTags
{
    // Use docker.io registry by default (can be overridden by the hosting infra).
    internal const string Registry = "docker.io";

    // Official ngrok Docker Hub image
    // Use the official ngrok Docker Hub image by default. Change to a specific
    // tag for reproducible builds (for example, a specific semver or digest).
    internal const string Image = "ngrok/ngrok";

    // Default tag - change to a pinned tag when you need reproducible images.
    internal const string Tag = "latest";
}