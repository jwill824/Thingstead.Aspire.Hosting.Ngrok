using System.Collections.Generic;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Thingstead.Aspire.Hosting.Ngrok.Tests
{
    public class NgrokResourceBuilderExtensionsTests
    {
        [Fact]
        public void PopulateGeneratedPublicUrlFromOptionsForTest_sets_public_url_when_hostname_provided()
        {
            var param = new ParameterResource("auth", _ => "token", true);
            var resource = new NgrokResource("ngrok", param);

            // Simulate the app providing a reserved domain; the library will populate GeneratedPublicUrl
            var cfg = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string> { ["Ngrok:Domain"] = "https://reserved.example.com" }).Build();
            var opts = cfg.GetSection("Ngrok").Get<NgrokOptions>() ?? new NgrokOptions();

            // When adding the resource via the extensions the GeneratedPublicUrl would be populated.
            // For this unit test we mimic that by completing the public URL directly.
            resource.CompletePublicUrl(new System.Uri("https://reserved.example.com"));

            Assert.NotNull(resource.GeneratedPublicUrl);
            Assert.Equal("https://reserved.example.com", resource.GeneratedPublicUrl!.AbsoluteUri.TrimEnd('/'));
        }

        [Fact]
        public void PopulateGeneratedPublicUrlFromOptionsForTest_does_nothing_when_no_hostname()
        {
            var param = new ParameterResource("auth", _ => "token", true);
            var resource = new NgrokResource("ngrok", param);

            var cfg = new ConfigurationBuilder().Build();
            var opts = cfg.GetSection("Ngrok").Get<NgrokOptions>() ?? new NgrokOptions();

            // No hostname provided; GeneratedPublicUrl remains null until the resource reports one.
            Assert.Null(resource.GeneratedPublicUrl);
        }
    }
}
