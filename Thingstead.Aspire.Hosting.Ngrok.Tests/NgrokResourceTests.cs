using System.Threading.Tasks;
using Aspire.Hosting.ApplicationModel;

namespace Thingstead.Aspire.Hosting.Ngrok.Tests;

public class NgrokResourceTests
{
    [Fact]
    public async Task CompletePublicUrl_sets_generated_and_task_completes()
    {
        var param = new ParameterResource("auth", _ => "token", true);
        var r = new NgrokResource("ngrok", param);
        var uri = new Uri("http://example.com/");
        r.CompletePublicUrl(uri);
        var u = await r.Uri;
        Assert.Equal(uri, u);
    }
}
