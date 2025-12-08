namespace Aspire.Hosting
{
    /// <summary>
    /// Client for making inspection API requests to the ngrok agent.
    /// </summary>
    public interface IInspectionApiClient
    {
        /// <summary>
        /// Sends a GET request to the specified URI.
        /// </summary>
        /// <param name="uri">The target URI.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The HTTP response message.</returns>
        Task<HttpResponseMessage> GetAsync(Uri uri, CancellationToken cancellationToken);
    }
}
