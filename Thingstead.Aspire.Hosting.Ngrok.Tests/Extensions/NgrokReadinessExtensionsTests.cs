using Aspire.Hosting.ApplicationModel;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Thingstead.Aspire.Hosting.Ngrok.Tests.Extensions;

public class NgrokReadinessExtensionsTests
{
    [Fact]
    public void TryExtractFirstPublicUrlFromInspectionJson_returns_first_public_url()
    {
        var json = @"{
            ""tunnels"": [
                { ""name"": ""t1"", ""public_url"": ""https://first.ngrok.io"" },
                { ""name"": ""t2"", ""public_url"": ""https://second.ngrok.io"" }
            ]
        }";

        var actual = NgrokReadinessExtensions.TryExtractFirstPublicUrlFromInspectionJson(json);
        Assert.Equal("https://first.ngrok.io", actual);
    }

    [Fact]
    public void TryExtractFirstPublicUrlFromInspectionJson_returns_null_on_empty()
    {
        var actual = NgrokReadinessExtensions.TryExtractFirstPublicUrlFromInspectionJson(string.Empty);
        Assert.Null(actual);
    }

    [Fact]
    public void TryExtractFirstPublicUrlFromInspectionJson_returns_null_on_invalid_json()
    {
        var actual = NgrokReadinessExtensions.TryExtractFirstPublicUrlFromInspectionJson("not a json");
        Assert.Null(actual);
    }

    [Fact]
    public void TryExtractFirstPublicUrlFromInspectionJson_returns_null_when_no_tunnels()
    {
        var json = "{ \"foo\": 123 }";
        var actual = NgrokReadinessExtensions.TryExtractFirstPublicUrlFromInspectionJson(json);
        Assert.Null(actual);
    }

    [Fact]
    public void TryExtractFirstPublicUrlFromInspectionJson_handles_tunnels_without_public_url()
    {
        var json = "{ \"tunnels\": [ { \"name\": \"x\" }, { \"name\": \"y\" } ] }";
        var actual = NgrokReadinessExtensions.TryExtractFirstPublicUrlFromInspectionJson(json);
        Assert.Null(actual);
    }

    [Fact]
    public async Task GetGeneratedPublicUrlAsync_sets_generated_public_url_when_inspection_returns_tunnel()
    {
        var param = new ParameterResource("auth", _ => "token", true);
        var resource = new NgrokResource("ngrok", param);

        // create fake http response with a simple tunnels array
        var json = "{ \"tunnels\": [ { \"name\": \"t1\", \"public_url\": \"https://probe.ngrok.io\" } ] }";
        HttpResponseMessage ResponseFactory(Uri u, CancellationToken ct)
        {
            var resp = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json)
            };
            return resp;
        }

        var client = new TestInspectionApiClient((u, ct) => Task.FromResult(ResponseFactory(u, ct)));

        await NgrokReadinessExtensions.GetGeneratedPublicUrlAsync(resource, new("http://localhost:4041/api/tunnels"), pollTimeoutSeconds: 2, client: client, cancellationToken: CancellationToken.None, initialDelayMs: 0);

        var u = await resource.Uri;
        Assert.Equal(new Uri("https://probe.ngrok.io"), u);
    }

    [Fact]
    public async Task GetGeneratedPublicUrlAsync_does_not_set_public_url_on_empty_body()
    {
        var param = new ParameterResource("auth", _ => "token", true);
        var resource = new NgrokResource("ngrok", param);

        static HttpResponseMessage ResponseFactory(Uri u, CancellationToken ct)
        {
            var resp = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(string.Empty)
            };
            return resp;
        }

        var client = new TestInspectionApiClient((u, ct) => Task.FromResult(ResponseFactory(u, ct)));

        await NgrokReadinessExtensions.GetGeneratedPublicUrlAsync(resource, new("http://localhost:4041/api/tunnels"), pollTimeoutSeconds: 1, client: client, cancellationToken: CancellationToken.None, initialDelayMs: 0);

        Assert.False(resource.Uri.IsCompleted);
    }

    [Fact]
    public async Task GetGeneratedPublicUrlAsync_does_not_set_public_url_on_invalid_json()
    {
        var param = new ParameterResource("auth", _ => "token", true);
        var resource = new NgrokResource("ngrok", param);

        static HttpResponseMessage ResponseFactory(Uri u, CancellationToken ct)
        {
            var resp = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("not a json")
            };
            return resp;
        }

        var client = new TestInspectionApiClient((u, ct) => Task.FromResult(ResponseFactory(u, ct)));

        await NgrokReadinessExtensions.GetGeneratedPublicUrlAsync(resource, new("http://localhost:4041/api/tunnels"), pollTimeoutSeconds: 1, client: client, cancellationToken: CancellationToken.None, initialDelayMs: 0);

        Assert.False(resource.Uri.IsCompleted);
    }
}

internal sealed class TestInspectionApiClient(Func<Uri, CancellationToken, Task<HttpResponseMessage>> handler) : IHttpClientWrapper
{
    private readonly Func<Uri, CancellationToken, Task<HttpResponseMessage>> _handler = handler;

    public Task<HttpResponseMessage> GetAsync(Uri uri, CancellationToken cancellationToken) => _handler(uri, cancellationToken);
}
