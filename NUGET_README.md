# Thingstead.Aspire.Hosting.Ngrok

Ngrok integration helpers for Aspire hosting environments. This package provides a small test harness resource
that runs ngrok in a container and exposes a public URL to the host when a tunnel is ready.

## Usage

The following snippet shows a typical usage pattern. Add a parameter to hold the ngrok auth token,
add an `Ngrok` resource, configure the default forwarding command, and wait for the public URL.

```csharp
var ngrokAuthParam = builder.AddParameter("NgrokAuthToken", secret: true);
var ngrok = builder.AddNgrok("ngrok", authToken: ngrokAuthParam, logger: AppHostLogger.Info)
    .WithDefaultCommand("host.docker.internal", int.Parse(YARP_PORT))
    .OnResourceReady(async (r, e, c) =>
    {
        AppHostLogger.Info("Waiting for ngrok to publish public URL...");

        try
        {
            var url = await r.PublicUrlTask;
            if (url is null)
            {
                AppHostLogger.Error("Ngrok did not publish a public URL within the timeout");
                return;
            }

            AppHostLogger.Info("Ngrok public URL: " + url);

            try
            {
                await QrUtil.GenerateAsync($"exp://{url.Host}", qrCodeFile);
                AppHostLogger.Info($"Generated QR for exp://{url.Host} at {qrCodeFile}");
            }
            catch (Exception ex)
            {
                AppHostLogger.Error($"Failed to generate QR: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            AppHostLogger.Error($"Error waiting for ngrok public URL: {ex.Message}");
        }
    });
```

## Notes

- The resource will set the configured ngrok auth token into the container environment. Provide the token via a secret `ParameterResource` created with `AddParameter(..., secret: true)`.
- The `PublicUrlTask` completes when the host-side probing logic discovers a tunnel public URL via the ngrok inspection API.
- Consider pinning the container image tag in your consuming project for reproducible runs.
