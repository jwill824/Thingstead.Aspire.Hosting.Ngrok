namespace Thingstead.Aspire.Hosting.Ngrok.Tests.Builders;

public class NgrokArgumentBuilderTests
{
    [Fact]
    public void BuildArgs_http_with_hostname_includes_hostname_arg()
    {
        var ngrokOpts = new NgrokOptions { Plan = "hobbyist", Domain = "https://example.com", Mode = "http" };
        var args = NgrokArgumentBuilder.BuildArgs(ngrokOpts);
        Assert.Contains(args, a => a.StartsWith("--hostname=") || a.Contains("example.com"));
    }

    [Fact]
    public void BuildArgs_http_without_hostname_returns_http_targeting_yarp()
    {
        var ngrokOpts = new NgrokOptions { Plan = "free", TargetHostname = "host.docker.internal", TargetHostnamePort = 8080 };
        var args = NgrokArgumentBuilder.BuildArgs(ngrokOpts);
        Assert.Contains("http", args);
        Assert.Contains(args, a => a.Contains("host.docker.internal") && a.Contains("8080"));
    }
}
