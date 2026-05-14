// <copyright file="Plugin.cs" company="npdib ltd">
// Copyright (c) npdib ltd. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Jellyfin.Plugin.Template;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Jellyfin.Plugin.Template.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

/// <summary>
/// The main plugin.
/// </summary>
public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    private const string Marker = "PSE-LOADER";

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

    /// <summary>
    /// Initializes a new instance of the <see cref="Plugin"/> class.
    /// </summary>
    /// <param name="applicationPaths">Instance of the <see cref="IApplicationPaths"/> interface.</param>
    /// <param name="xmlSerializer">Instance of the <see cref="IXmlSerializer"/> interface.</param>
    public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
        : base(applicationPaths, xmlSerializer)
    {
        Instance = this;
        InstallLoader(applicationPaths.WebPath);
    }

    /// <summary>
    /// Gets the current plugin instance.
    /// </summary>
    public static Plugin? Instance { get; private set; }

    /// <inheritdoc />
    public override string Name => "Password Strength Enforcement";

    /// <inheritdoc />
    public override Guid Id => Guid.Parse("5a2d0001-12cf-4444-b2e3-e2f8e1f54e34");

    /// <inheritdoc />
    public IEnumerable<PluginPageInfo> GetPages()
    {
        return
        [
            new PluginPageInfo
            {
                Name = this.Name,
                EmbeddedResourcePath = string.Format(CultureInfo.InvariantCulture, "{0}.Configuration.configPage.html", this.GetType().Namespace),
            },
        ];
    }

    private static void InstallLoader(string webPath)
    {
        try
        {
            if (string.IsNullOrEmpty(webPath) || !Directory.Exists(webPath))
            {
                return;
            }

            var customJsPath = Path.Combine(webPath, "custom.js");

            if (File.Exists(customJsPath))
            {
                var existing = File.ReadAllText(customJsPath);
                if (existing.Contains(Marker, StringComparison.Ordinal))
                {
                    return;
                }

                File.AppendAllText(customJsPath, Loader);
            }
            else
            {
                File.WriteAllText(customJsPath, Loader.TrimStart('\n'));
            }
        }
        catch (UnauthorizedAccessException)
        {
            // Web directory is read-only (e.g. Docker image layer) — nothing we can do.
        }
        catch (IOException)
        {
            // Best-effort; don't crash plugin load.
        }
    }
}
