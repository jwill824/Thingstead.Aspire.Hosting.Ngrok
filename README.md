# Thingstead.Aspire.Hosting.Ngrok

Helpers to integrate ngrok with Aspire hosting.

## Overview

This project produces a NuGet package and publishes it to GitHub Packages via GitHub Actions. The workflow automatically applies semantic versioning and creates GitHub Releases with release notes.

## Consuming

This repository provides `NuGet.config.template` with the GitHub Packages feed URL and `packageSourceMapping` for `Thingstead.Aspire.Hosting.Ngrok`.

Do NOT commit a `NuGet.config` that contains credentials.
Recommended minimal flow (manual PAT insertion via a password manager):

1. Create a GitHub Personal Access Token (PAT) with the minimum scope you need — for example: `read:packages` to consume packages, or `write:packages` to publish (add `repo` if required).

2. Store the PAT in your password manager and copy it when needed.

3. Create a local `NuGet.config` from `NuGet.config.template` and add the PAT into the credentials block (do NOT commit this file).

4. Restore or add the package locally:

```bash
DOTNET_CLI_TELEMETRY_OPTOUT=1 dotnet restore
# or
dotnet add package Thingstead.Aspire.Hosting.Ngrok --version 0.1.0
```

## Usage

Add an ngrok resource to your Aspire distributed application. Typical flow:

- Create a secret parameter to hold the ngrok auth token
- Bind `NgrokOptions` from configuration (or construct manually)
- Add the resource with `AddNgrok(...)` and configure container environment variables
- Wait for the resource to publish a public Url by awaiting the resource's `Uri` task

Example:

```csharp
var ngrokAuthParam = builder.AddParameter("NgrokAuthToken", secret: true);
// Create options from IConfiguration/environment by binding to the NgrokOptions POCO
var ngrokOptions = builder.Configuration.GetSection("Ngrok").Get<NgrokOptions>() ?? new NgrokOptions();

var ngrokBuilder = builder.AddNgrok("ngrok", ngrokOptions, authToken: ngrokAuthParam);
```

Notes:

- `AddNgrok` requires the auth token parameter (the return value from `builder.AddParameter`) and an `NgrokOptions` instance which controls how the container is configured (target port/hostname, mode, domain/hostname reservation, etc.).
- If you provide a reserved domain/hostname via `NgrokOptions.Domain` (or set `Hostname` directly), `AddNgrok` will populate the resource's published Uri immediately from that hostname.
- For the free/http plan without a reserved hostname, the host-side harness will poll the ngrok inspection API and the resource's published Uri will be populated once a tunnel `public_url` is discovered.
- The auth token parameter you pass is wired into the container environment by `AddNgrok` (it sets `NGROK_AUTHTOKEN` internally) so consumers generally do not need to set it manually.

> [!NOTE]
> The resource will set the configured ngrok auth token into the container environment. Provide the token via a secret `ParameterResource` created with `AddParameter(..., secret: true)`.

> [!NOTE]
> The resource exposes a Task-based `Uri` property (named `Uri`) which completes when the host-side probing logic discovers a tunnel public URL via the ngrok inspection API.

> [!NOTE]
> Consider pinning the container image tag in your consuming project for reproducible runs.

## Contributing

See `CONTRIBUTING.md` for contribution guidelines, testing instructions, and release details.
