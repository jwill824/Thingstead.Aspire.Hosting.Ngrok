// For ease of discovery, resource types should be placed in
// the Aspire.Hosting.ApplicationModel namespace. If there is
// likelihood of a conflict on the resource name consider using
// an alternative namespace.
namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents an ngrok container resource managed by the Aspire hosting test harness.
/// The resource exposes an auth-token parameter and provides a <see cref="Uri"/>
/// task that completes when ngrok has published a public URL for the tunneled endpoint.
/// </summary>
public sealed class NgrokResource(string name, ParameterResource authToken) : ContainerResource(name)
{
    /// <summary>
    /// Gets the parameter resource that contains the ngrok auth token (passed into the container
    /// as an environment variable by the host).
    /// </summary>
    public ParameterResource AuthTokenParameter { get; } = authToken;

    // Task that completes when a public URL has been discovered for this resource.
    private readonly TaskCompletionSource<Uri?> _publicUrlTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>
    /// Internal API: signal that the ngrok resource has discovered a public URL. This is invoked by the
    /// host-side probing logic and should not be called by consumers.
    /// </summary>
    /// <param name="url">The discovered public URL.</param>
    internal void CompletePublicUrl(Uri url)
    {
        try
        {
            _publicUrlTcs.TrySetResult(url);
        }
        catch { /* best-effort */ }
    }

    /// <summary>
    /// Task that yields the discovered public Uri (or null when not available yet).
    /// Consumers can await this task to get the published Uri once it's discovered.
    /// </summary>
    public Task<Uri?> Uri => _publicUrlTcs.Task;
}