namespace Aspire.Hosting
{
    /// <summary>
    /// Default implementation of <see cref="IInspectionApiClient"/> using <see cref="HttpClient"/>.
    /// </summary>
    public sealed class DefaultInspectionApiClient : IInspectionApiClient, IDisposable
    {
        private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(10) };

        /// <inheritdoc/>
        public Task<HttpResponseMessage> GetAsync(Uri uri, CancellationToken cancellationToken)
            => _http.GetAsync(uri, cancellationToken);

        /// <inheritdoc/>
        public void Dispose() => _http.Dispose();
    }
}
