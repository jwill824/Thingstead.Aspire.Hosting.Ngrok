using System.Collections.Generic;
using System.Threading.Tasks;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Configuration;

namespace Thingstead.Aspire.Hosting.Ngrok.Tests.Extensions;

public class NgrokResourceBuilderExtensionsTests
{
    [Fact]
    public async Task PopulateGeneratedPublicUrlFromOptionsForTest_sets_public_url_when_hostname_provided()
    {
        var param = new ParameterResource("auth", _ => "token", true);
        var resource = new NgrokResource("ngrok", param);

        var cfg = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string> { ["Ngrok:Domain"] = "https://reserved.example.com" }).Build();
        var opts = cfg.GetSection("Ngrok").Get<NgrokOptions>() ?? new NgrokOptions();

        resource.CompletePublicUrl(new System.Uri("https://reserved.example.com"));

        var u = await resource.Uri;
        Assert.NotNull(u);
        Assert.Equal("https://reserved.example.com", u!.AbsoluteUri.TrimEnd('/'));
    }

    [Fact]
    public async Task PopulateGeneratedPublicUrlFromOptionsForTest_does_nothing_when_no_hostname()
    {
        var param = new ParameterResource("auth", _ => "token", true);
        var resource = new NgrokResource("ngrok", param);

        var cfg = new ConfigurationBuilder().Build();
        var opts = cfg.GetSection("Ngrok").Get<NgrokOptions>() ?? new NgrokOptions();

        Assert.False(resource.Uri.IsCompleted);
    }
}
