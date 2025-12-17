namespace Thingstead.Aspire.Hosting.Ngrok.Tests.Builders;

public class NgrokArgumentBuilderTests
{
    [Theory]
    [InlineData(NgrokPlan.Hobbyist, NgrokMode.Http)]
    [InlineData(NgrokPlan.Free, NgrokMode.Http)]
    public void BuildArgs_http_hobbyist_plan_returns_args(NgrokPlan plan, NgrokMode mode)
    {
        var ngrokOpts = new NgrokOptions { Plan = plan, Mode = mode, TargetHostname = "host.docker.internal", TargetHostnamePort = 8080 };
        var args = NgrokArgumentBuilder.BuildArgs(ngrokOpts);
        Assert.Contains([
            "http",
            "http://host.docker.internal:8080",
            "--log=stdout",
            "--log-level=debug"
        ], args);
    }

    [Fact]
    public void BuildArgs_http_free_plan_not_supported()
    {
        var ngrokOpts = new NgrokOptions { Plan = NgrokPlan.PayAsYouGo };
        Assert.Throws<NotImplementedException>(() => NgrokArgumentBuilder.BuildArgs(ngrokOpts));
    }
}
