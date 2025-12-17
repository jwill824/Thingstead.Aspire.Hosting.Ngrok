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

        if (options.Mode == NgrokMode.Http && options.Plan == NgrokPlan.Free)
        {
            ngrokBuilder = ngrokBuilder.WaitForGeneratedPublicUrl();
        }

        var args = NgrokArgumentBuilder.BuildArgs(options);
        ngrokBuilder = ngrokBuilder.WithArgs(args);

        if (!string.IsNullOrWhiteSpace(options.Domain))
        {
            resource.CompletePublicUrl(new Uri(options.Domain));
        }

        return ngrokBuilder;
    }
}

/// <summary>
/// Ngrok <seealso href="https://ngrok.com/docs/pricing-limits">plans</seealso> supported by the Ngrok hosting resource.
/// </summary>
public enum NgrokPlan
{
    /// <summary>
    /// Free plan.
    /// </summary>
    Free,
    /// <summary>
    /// Hobbyist plan.
    /// </summary>
    Hobbyist,
    /// <summary>
    /// Pay-as-you-go plan.
    /// </summary>
    PayAsYouGo
}

/// <summary>
/// Ngrok <seealso href="https://ngrok.com/docs/universal-gateway/protocols">modes</seealso> supported by the Ngrok hosting resource.
/// </summary>
public enum NgrokMode
{
    /// <summary>
    /// See <seealso href="https://ngrok.com/docs/universal-gateway/http">HTTP mode</seealso>.
    /// </summary>
    Http,
    /// <summary>
    /// See <seealso href="https://ngrok.com/docs/universal-gateway/tcp">TCP mode</seealso>.
    /// </summary>
    Tcp,
    /// <summary>
    /// See <seealso href="https://ngrok.com/docs/universal-gateway/tls">TLS mode</seealso>.
    /// </summary>
    Tls
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