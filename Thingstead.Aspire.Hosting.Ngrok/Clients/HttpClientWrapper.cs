namespace Aspire.Hosting;

/// <summary>
/// Client for making inspection API requests to the ngrok agent.
/// </summary>
public interface IHttpClientWrapper
{
    /// <summary>
    /// Sends a GET request to the specified URI.
    /// </summary>
    /// <param name="uri">The target URI.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The HTTP response message.</returns>
    Task<HttpResponseMessage> GetAsync(Uri uri, CancellationToken cancellationToken);
}

/// <summary>
/// Default implementation of <see cref="IHttpClientWrapper"/> using <see cref="System.Net.Http.HttpClient"/>.
/// </summary>
public sealed class HttpClientWrapper : IHttpClientWrapper, IDisposable
{
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(10) };

    /// <inheritdoc/>
    public Task<HttpResponseMessage> GetAsync(Uri uri, CancellationToken cancellationToken)
        => _http.GetAsync(uri, cancellationToken);

    /// <inheritdoc/>
    public void Dispose() => _http.Dispose();
}
