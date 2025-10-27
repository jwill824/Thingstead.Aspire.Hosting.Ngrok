// For ease of discovery, resource types should be placed in
// the Aspire.Hosting.ApplicationModel namespace. If there is
// likelihood of a conflict on the resource name consider using
// an alternative namespace.
namespace Aspire.Hosting.ApplicationModel;

public sealed class NgrokResource(string name, ParameterResource authToken) : ContainerResource(name)
{
    /// <summary>
    /// Gets the parameter that contains the Ngrok auth token.
    /// </summary>
    public ParameterResource AuthTokenParameter { get; } = authToken;

    // Task that completes when a public URL has been discovered for this resource.
    private readonly TaskCompletionSource<Uri?> _publicUrlTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>
    /// Task that completes when the ngrok public url is known.
    /// </summary>
    public Task<Uri?> PublicUrlTask => _publicUrlTcs.Task;

    internal void CompletePublicUrl(Uri url)
    {
        try
        {
            GeneratedPublicUrl = url;
            _publicUrlTcs.TrySetResult(url);
        }
        catch { /* best-effort */ }
    }

    public Uri GeneratedPublicUrl { get; set; } = null!;
}