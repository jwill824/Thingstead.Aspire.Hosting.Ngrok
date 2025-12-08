using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Thingstead.Aspire.Hosting.Ngrok.Tests;

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
    public async Task ProbeInspectionApiCandidatesAsync_sets_generated_public_url_when_inspection_returns_tunnel()
    {
        var param = new ParameterResource("auth", _ => "token", true);
        var resource = new NgrokResource("ngrok", param);

        var candidates = new List<Uri> { new("http://localhost:4041/api/tunnels") };

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

        await NgrokReadinessExtensions.ProbeInspectionApiCandidatesAsync(resource, candidates, pollTimeoutSeconds: 2, logger: NullLogger.Instance, client: client, cancellationToken: CancellationToken.None, initialDelayMs: 0);

        Assert.Equal(new Uri("https://probe.ngrok.io"), resource.GeneratedPublicUrl);
    }

    [Fact]
    public async Task ProbeInspectionApiCandidatesAsync_does_not_set_public_url_on_empty_body()
    {
        var param = new ParameterResource("auth", _ => "token", true);
        var resource = new NgrokResource("ngrok", param);

        var candidates = new List<Uri> { new("http://localhost:4041/api/tunnels") };

        HttpResponseMessage ResponseFactory(Uri u, CancellationToken ct)
        {
            var resp = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(string.Empty)
            };
            return resp;
        }

        var client = new TestInspectionApiClient((u, ct) => Task.FromResult(ResponseFactory(u, ct)));

        await NgrokReadinessExtensions.ProbeInspectionApiCandidatesAsync(resource, candidates, pollTimeoutSeconds: 1, logger: NullLogger.Instance, client: client, cancellationToken: CancellationToken.None, initialDelayMs: 0);

        Assert.Null(resource.GeneratedPublicUrl);
    }

    [Fact]
    public async Task ProbeInspectionApiCandidatesAsync_skips_failed_candidate_and_uses_next()
    {
        var param = new ParameterResource("auth", _ => "token", true);
        var resource = new NgrokResource("ngrok", param);

        var candidates = new List<Uri>
        {
            new("http://first:4041/api/tunnels"),
            new("http://second:4041/api/tunnels")
        };

        var json = "{ \"tunnels\": [ { \"name\": \"t1\", \"public_url\": \"https://second.ngrok.io\" } ] }";

        Task<HttpResponseMessage> Handler(Uri u, CancellationToken ct)
        {
            if (u.Host.Equals("first", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromException<HttpResponseMessage>(new HttpRequestException("connect failed"));
            }

            var resp = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json)
            };
            return Task.FromResult(resp);
        }

        var client = new TestInspectionApiClient((u, ct) => Handler(u, ct));

        await NgrokReadinessExtensions.ProbeInspectionApiCandidatesAsync(resource, candidates, pollTimeoutSeconds: 2, logger: NullLogger.Instance, client: client, cancellationToken: CancellationToken.None, initialDelayMs: 0);

        Assert.Equal(new Uri("https://second.ngrok.io"), resource.GeneratedPublicUrl);
    }

    [Fact]
    public async Task ProbeInspectionApiCandidatesAsync_does_not_set_public_url_on_invalid_json()
    {
        var param = new ParameterResource("auth", _ => "token", true);
        var resource = new NgrokResource("ngrok", param);

        var candidates = new List<Uri> { new("http://localhost:4041/api/tunnels") };

        HttpResponseMessage ResponseFactory(Uri u, CancellationToken ct)
        {
            var resp = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("not a json")
            };
            return resp;
        }

        var client = new TestInspectionApiClient((u, ct) => Task.FromResult(ResponseFactory(u, ct)));

        await NgrokReadinessExtensions.ProbeInspectionApiCandidatesAsync(resource, candidates, pollTimeoutSeconds: 1, logger: NullLogger.Instance, client: client, cancellationToken: CancellationToken.None, initialDelayMs: 0);

        Assert.Null(resource.GeneratedPublicUrl);
    }
}

internal sealed class TestInspectionApiClient(Func<Uri, CancellationToken, Task<HttpResponseMessage>> handler) : IInspectionApiClient
{
    private readonly Func<Uri, CancellationToken, Task<HttpResponseMessage>> _handler = handler;

    public Task<HttpResponseMessage> GetAsync(Uri uri, CancellationToken cancellationToken) => _handler(uri, cancellationToken);
}
