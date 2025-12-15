using System.Threading.Tasks;
using Aspire.Hosting.ApplicationModel;

namespace Thingstead.Aspire.Hosting.Ngrok.Tests.Extensions;

public class NgrokResourceBuilderExtensionsTests
{
    [Fact]
    public async Task PopulateGeneratedPublicUrlFromOptionsForTest_sets_public_url_when_hostname_provided()
    {
        var param = new ParameterResource("auth", _ => "token", true);
        var resource = new NgrokResource("ngrok", param);

        resource.CompletePublicUrl(new Uri("https://reserved.example.com"));

        var u = await resource.Uri;
        Assert.NotNull(u);
        Assert.Equal("https://reserved.example.com", u!.AbsoluteUri.TrimEnd('/'));
    }

    [Fact]
    public async Task PopulateGeneratedPublicUrlFromOptionsForTest_does_nothing_when_no_hostname()
    {
        var param = new ParameterResource("auth", _ => "token", true);
        var resource = new NgrokResource("ngrok", param);

        Assert.False(resource.Uri.IsCompleted);
    }

    [Fact]
    public void BuildPublicUrlFromOptions_returns_null_when_no_domain()
    {
        var opts = new NgrokOptions();
        var u = NgrokResourceBuilderExtensions.BuildPublicUrlFromOptions(opts);
        Assert.Null(u);
    }

    [Fact]
    public void BuildPublicUrlFromOptions_handles_domain_with_scheme()
    {
        var opts = new NgrokOptions { Domain = "https://reserved.example.com" };
        var u = NgrokResourceBuilderExtensions.BuildPublicUrlFromOptions(opts);
        Assert.Equal(new Uri("https://reserved.example.com"), u);
    }

    [Fact]
    public void BuildPublicUrlFromOptions_infers_https_for_tls_mode_when_domain_has_no_scheme()
    {
        var opts = new NgrokOptions { Domain = "reserved.example.com", Mode = "tls" };
        var u = NgrokResourceBuilderExtensions.BuildPublicUrlFromOptions(opts);
        Assert.Equal(new Uri("https://reserved.example.com"), u);
    }

    [Fact]
    public void BuildPublicUrlFromOptions_uses_hostname_override_when_provided()
    {
        var opts = new NgrokOptions { Domain = "https://reserved.example.com", Hostname = "explicit.host" };
        var u = NgrokResourceBuilderExtensions.BuildPublicUrlFromOptions(opts);
        Assert.Equal(new Uri("https://explicit.host"), u);
    }
}
