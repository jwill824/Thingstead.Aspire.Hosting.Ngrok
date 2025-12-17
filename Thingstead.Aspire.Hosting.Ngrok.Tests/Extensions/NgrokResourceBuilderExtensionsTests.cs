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
}
