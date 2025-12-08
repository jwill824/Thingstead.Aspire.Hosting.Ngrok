using System;
using Aspire.Hosting.ApplicationModel;
using Xunit;

namespace Thingstead.Aspire.Hosting.Ngrok.Tests;

public class NgrokResourceTests
{
    [Fact]
    public async System.Threading.Tasks.Task CompletePublicUrl_sets_generated_and_task_completes()
    {
        var param = new ParameterResource("auth", _ => "token", true);
        var r = new NgrokResource("ngrok", param);
        var t = r.PublicUrlTaskString;
        var uri = new Uri("http://example.com/");
        r.CompletePublicUrl(uri);
        var s = await t;
        Assert.Equal("http://example.com", s);
        Assert.Equal(uri, r.GeneratedPublicUrl);
    }
}
