using System;
using Aspire.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Thingstead.Aspire.Hosting.Ngrok.Tests;

public class NgrokArgumentBuilderTests
{
    [Fact]
    public void BuildArgs_http_with_hostname_includes_hostname_arg()
    {
        var ngrokOpts = new NgrokOptions { Plan = "hobbyist", Domain = "example.com" };
        var args = NgrokArgumentBuilder.BuildArgs(ngrokOpts, NullLogger.Instance);
        Assert.Contains(args, a => a.StartsWith("--hostname=") || a.Contains("example.com"));
    }

    [Fact]
    public void BuildArgs_http_without_hostname_returns_http_targeting_yarp()
    {
        var ngrokOpts = new NgrokOptions { Plan = "free" };
        // ensure environment fallback is used for internal url and port
        Environment.SetEnvironmentVariable("NGROK_INTERNAL_URL", "host.docker.internal");
        Environment.SetEnvironmentVariable("YARP_PORT", "8080");
        var args = NgrokArgumentBuilder.BuildArgs(ngrokOpts, NullLogger.Instance);
        Assert.Contains("http", args);
        Assert.Contains(args, a => a.Contains("host.docker.internal") && a.Contains("8080"));
    }

    [Fact]
    public void BuildArgs_tls_with_hostname_uses_tls_and_hostname()
    {
        var ngrokOpts = new NgrokOptions { Plan = "pay-as-you-go", Domain = "example.com" };
        var args = NgrokArgumentBuilder.BuildArgs(ngrokOpts, NullLogger.Instance);
        Assert.Contains("tls", args);
        Assert.Contains(args, a => a.StartsWith("--hostname=") && a.Contains("example.com"));
    }

    [Fact]
    public void BuildArgs_tcp_uses_tcp_and_expo_port()
    {
        Environment.SetEnvironmentVariable("EXPO_PORT", "12345");
        var ngrokOpts = new NgrokOptions { Mode = "tcp" };
        var args = NgrokArgumentBuilder.BuildArgs(ngrokOpts, NullLogger.Instance);
        Assert.Contains("tcp", args);
        Assert.Contains("12345", args);
    }
}
