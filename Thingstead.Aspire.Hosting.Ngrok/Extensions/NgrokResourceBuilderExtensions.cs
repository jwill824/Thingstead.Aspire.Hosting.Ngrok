// Put extensions in the Aspire.Hosting namespace to ease discovery as referencing
// the Aspire hosting package automatically adds this namespace.
namespace Aspire.Hosting;

/// <summary>
/// Extension methods for adding and configuring an <see cref="NgrokResource"/> in an
/// <see cref="IDistributedApplicationBuilder"/>. These are placed in the <c>Aspire.Hosting</c>
/// namespace for discoverability when referencing the Aspire hosting packages.
/// </summary>
public static class NgrokResourceBuilderExtensions
{
    private const string NGROK_AUTHTOKEN = nameof(NGROK_AUTHTOKEN);

    /// <summary>
    /// Adds an <see cref="NgrokResource"/> with options and an optional environment configuration action.
    /// Callers typically create an <see cref="NgrokOptions"/> by binding the application's configuration
    /// (for example: <c>builder.Configuration.GetSection("Ngrok").Get&lt;NgrokOptions&gt;()</c>), or pass <c>null</c>
    /// to use configuration discovered from the distributed application builder.
    /// </summary>
    /// <param name="builder">The distributed application builder.</param>
    /// <param name="options">Ngrok Options.</param>
    /// <param name="authToken">Parameter resource that provides the ngrok auth token.</param>
    public static IResourceBuilder<NgrokResource> AddNgrok(
        this IDistributedApplicationBuilder builder,
        NgrokOptions options,
        IResourceBuilder<ParameterResource> authToken)
    {
        ArgumentNullException.ThrowIfNull(builder);
        var resource = new NgrokResource(options.ResourceName, authToken?.Resource ?? throw new ArgumentNullException(nameof(authToken)));
        var ngrokBuilder = builder!.AddResource(resource)
            .WithImage(NgrokContainerImageTags.Image)
            .WithImageRegistry(NgrokContainerImageTags.Registry)
            .WithImageTag(NgrokContainerImageTags.Tag)
            .WithHttpEndpoint(targetPort: options.TargetPort, port: options.Port)
            .WithEnvironment(context =>
            {
                context.EnvironmentVariables[NGROK_AUTHTOKEN] = resource.AuthTokenParameter;
            });

        if (string.Equals(options.Mode, "http", StringComparison.OrdinalIgnoreCase) && string.Equals(options.Plan, "free", StringComparison.OrdinalIgnoreCase))
        {
            ngrokBuilder = ngrokBuilder.WaitForGeneratedPublicUrl();
        }

        var args = NgrokArgumentBuilder.BuildArgs(options);
        ngrokBuilder = ngrokBuilder.WithArgs(args);

        if (!string.IsNullOrWhiteSpace(options.Domain))
        {
            try
            {
                var publicUrl = BuildPublicUrlFromOptions(options);
                if (publicUrl is not null)
                {
                    resource.CompletePublicUrl(publicUrl);
                }
            }
            catch
            {
                // best-effort: ignore failures when computing a public URL from options
            }
        }

        return ngrokBuilder;
    }

    internal static Uri? BuildPublicUrlFromOptions(NgrokOptions options)
    {
        if (options is null || string.IsNullOrWhiteSpace(options.Domain)) return null;

        Uri? parsedDomain = null;
        if (Uri.TryCreate(options.Domain, UriKind.Absolute, out var pd) && !string.IsNullOrEmpty(pd.Scheme))
        {
            parsedDomain = pd;
        }

        string scheme;
        if (parsedDomain is not null)
        {
            scheme = parsedDomain.Scheme;
        }
        else if (!string.IsNullOrWhiteSpace(options.Mode))
        {
            scheme = string.Equals(options.Mode, "tls", StringComparison.OrdinalIgnoreCase) ? "https" : "http";
        }
        else
        {
            scheme = "https";
        }

        var host = !string.IsNullOrWhiteSpace(options.Hostname)
            ? options.Hostname
            : parsedDomain?.Host ?? options.Domain;

        return new Uri($"{scheme}://{host}");
    }    
}

// This class just contains constant strings that can be updated periodically
// when new versions of the underlying container are released.
internal static class NgrokContainerImageTags
{
    // Use docker.io registry by default (can be overridden by the hosting infra).
    internal const string Registry = "docker.io";

    // Official ngrok Docker Hub image
    // Use the official ngrok Docker Hub image by default. Change to a specific
    // tag for reproducible builds (for example, a specific semver or digest).
    internal const string Image = "ngrok/ngrok";

    // Default tag - change to a pinned tag when you need reproducible images.
    internal const string Tag = "latest";
}