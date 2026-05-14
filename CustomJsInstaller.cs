// <copyright file="CustomJsInstaller.cs" company="npdib ltd">
// Copyright (c) npdib ltd. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Jellyfin.Plugin.Template;

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

/// <summary>
/// Hosted service that runs at server startup and ensures the enforcement
/// loader is present in the Jellyfin web root's custom.js.
/// This removes the need for any manual file editing on the server.
/// </summary>
public sealed class CustomJsInstaller : IHostedService
{
    /// <summary>
    /// Unique marker written into custom.js so duplicate installs are detected.
    /// </summary>
    private const string Marker = "PSE-LOADER";

    /// <summary>
    /// The loader appended to (or written into) custom.js.
    /// Uses ApiClient.serverAddress() when available, falls back to
    /// window.location.origin so it works regardless of reverse-proxy base path.
    /// </summary>
    private const string Loader =
        "\n// PSE-LOADER — Password Strength Enforcement (managed by plugin, do not remove)\n" +
        "(function(){" +
        "var b=(typeof ApiClient!=='undefined'&&ApiClient.serverAddress?" +
        "ApiClient.serverAddress().replace(/\\/$/,''):" +
        "window.location.origin||'');" +
        "var s=document.createElement('script');" +
        "s.src=b+'/PasswordStrength/enforcement.js';" +
        "s.defer=true;" +
        "document.head.appendChild(s);" +
        "}());\n";

    private readonly IApplicationPaths _applicationPaths;
    private readonly ILogger<CustomJsInstaller> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CustomJsInstaller"/> class.
    /// </summary>
    /// <param name="applicationPaths">Provides the server's directory paths.</param>
    /// <param name="logger">Logger instance.</param>
    public CustomJsInstaller(IApplicationPaths applicationPaths, ILogger<CustomJsInstaller> logger)
    {
        this._applicationPaths = applicationPaths;
        this._logger = logger;
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            var webPath = this._applicationPaths.WebPath;
            if (string.IsNullOrEmpty(webPath) || !Directory.Exists(webPath))
            {
                this._logger.LogWarning(
                    "PSE: Web path not found at '{WebPath}' — custom.js cannot be installed automatically.",
                    webPath);
                return;
            }

            var customJsPath = Path.Combine(webPath, "custom.js");

            if (File.Exists(customJsPath))
            {
                var existing = await File.ReadAllTextAsync(customJsPath, cancellationToken).ConfigureAwait(false);
                if (existing.Contains(Marker, StringComparison.Ordinal))
                {
                    this._logger.LogDebug("PSE: Enforcement loader already present in custom.js.");
                    return;
                }

                await File.AppendAllTextAsync(customJsPath, Loader, cancellationToken).ConfigureAwait(false);
                this._logger.LogInformation("PSE: Appended enforcement loader to existing custom.js.");
            }
            else
            {
                await File.WriteAllTextAsync(customJsPath, Loader.TrimStart('\n'), cancellationToken).ConfigureAwait(false);
                this._logger.LogInformation("PSE: Created custom.js with enforcement loader.");
            }
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "PSE: Failed to install enforcement loader into custom.js.");
        }
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
