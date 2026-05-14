// <copyright file="Plugin.cs" company="Nicholas Dibb-Fuller">
// Copyright (c) Nicholas Dibb-Fuller. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Jellyfin.Plugin.Template;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Loader;
using Jellyfin.Plugin.Template.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using Newtonsoft.Json.Linq;

/// <summary>
/// The main plugin.
/// </summary>
public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    private static readonly object SyncRoot = new object();

    /// <summary>
    /// Initializes a new instance of the <see cref="Plugin"/> class.
    /// </summary>
    /// <param name="applicationPaths">Instance of the <see cref="IApplicationPaths"/> interface.</param>
    /// <param name="xmlSerializer">Instance of the <see cref="IXmlSerializer"/> interface.</param>
    public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
        : base(applicationPaths, xmlSerializer)
    {
        Instance = this;
        this.RegisterIndexTransformation();
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

    /// <summary>Thread-safe check: returns true if the user is flagged for a forced reset.</summary>
    /// <param name="userIdN">User ID in format N (no hyphens).</param>
    /// <returns>True if the user must reset their password.</returns>
    internal static bool IsUserFlaggedForReset(string userIdN)
    {
        lock (SyncRoot)
        {
            return Instance?.Configuration.ForcedResetUserIds.Contains(userIdN) ?? false;
        }
    }

    /// <summary>Thread-safe: clears the forced-reset list and replaces it with <paramref name="userIds"/>.</summary>
    /// <param name="userIds">The new set of user IDs (format N) to flag.</param>
    internal static void SetForcedResetUsers(IEnumerable<string> userIds)
    {
        lock (SyncRoot)
        {
            var list = Instance!.Configuration.ForcedResetUserIds;
            list.Clear();
            foreach (var id in userIds)
            {
                list.Add(id);
            }
        }
    }

    /// <summary>Thread-safe: adds <paramref name="userIdN"/> to the forced-reset list (no-op if already present).</summary>
    /// <param name="userIdN">User ID in format N (no hyphens).</param>
    internal static void AddForcedResetUser(string userIdN)
    {
        lock (SyncRoot)
        {
            var list = Instance!.Configuration.ForcedResetUserIds;
            if (!list.Contains(userIdN))
            {
                list.Add(userIdN);
            }
        }
    }

    /// <summary>Thread-safe: removes <paramref name="userIdN"/> from the forced-reset list.</summary>
    /// <param name="userIdN">User ID in format N (no hyphens).</param>
    internal static void RemoveForcedResetUser(string userIdN)
    {
        lock (SyncRoot)
        {
            Instance!.Configuration.ForcedResetUserIds.Remove(userIdN);
        }
    }

    private void RegisterIndexTransformation()
    {
        var fileTransformAssembly = AssemblyLoadContext.All
            .SelectMany(x => x.Assemblies)
            .FirstOrDefault(x => x.FullName?.Contains(".FileTransformation", StringComparison.Ordinal) ?? false);

        if (fileTransformAssembly is null)
        {
            return;
        }

        var pluginInterfaceType = fileTransformAssembly.GetType("Jellyfin.Plugin.FileTransformation.PluginInterface");
        if (pluginInterfaceType is null)
        {
            return;
        }

        var payload = new JObject
        {
            ["id"] = "3f8a1c2d-4e5b-6f70-89ab-cdef01234567",
            ["fileNamePattern"] = "index.html",
            ["callbackAssembly"] = this.GetType().Assembly.FullName,
            ["callbackClass"] = typeof(FileTransformCallback).FullName,
            ["callbackMethod"] = nameof(FileTransformCallback.InjectLoader),
        };

        pluginInterfaceType.GetMethod("RegisterTransformation")?.Invoke(null, new object?[] { payload });
    }
}
