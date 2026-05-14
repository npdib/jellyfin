// <copyright file="PseStartupTask.cs" company="npdib ltd">
// Copyright (c) npdib ltd. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Jellyfin.Plugin.Template;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

/// <summary>
/// Scheduled startup task that registers the PSE index.html transformation with the
/// File Transformation plugin (https://github.com/IAmParadox27/jellyfin-plugin-file-transformation).
/// </summary>
public class PseStartupTask : IScheduledTask
{
    private static readonly Guid TransformationId = new Guid("3f8a1c2d-4e5b-6f70-89ab-cdef01234567");

    private readonly ILogger<PseStartupTask> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PseStartupTask"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    public PseStartupTask(ILogger<PseStartupTask> logger)
    {
        this._logger = logger;
    }

    /// <inheritdoc />
    public string Name => "Password Strength Enforcement Startup";

    /// <inheritdoc />
    public string Key => "Jellyfin.Plugin.Template.Startup";

    /// <inheritdoc />
    public string Description => "Registers the PSE enforcement script injection with the File Transformation plugin.";

    /// <inheritdoc />
    public string Category => "Startup Services";

    /// <inheritdoc />
    public Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        var fileTransformAssembly = AssemblyLoadContext.All
            .SelectMany(x => x.Assemblies)
            .FirstOrDefault(x => x.FullName?.Contains(".FileTransformation", StringComparison.Ordinal) ?? false);

        if (fileTransformAssembly is null)
        {
            this._logger.LogWarning(
                "PSE: File Transformation plugin not found. " +
                "Install it from https://www.iamparadox.dev/jellyfin/plugins/manifest.json to enable the password enforcement overlay.");
            return Task.CompletedTask;
        }

        var pluginInterfaceType = fileTransformAssembly.GetType("Jellyfin.Plugin.FileTransformation.PluginInterface");
        if (pluginInterfaceType is null)
        {
            this._logger.LogWarning("PSE: Could not locate PluginInterface type in File Transformation assembly.");
            return Task.CompletedTask;
        }

        var payload = new JObject
        {
            ["id"] = TransformationId.ToString(),
            ["fileNamePattern"] = "index.html",
            ["callbackAssembly"] = this.GetType().Assembly.FullName,
            ["callbackClass"] = typeof(FileTransformCallback).FullName,
            ["callbackMethod"] = nameof(FileTransformCallback.InjectLoader),
        };

        pluginInterfaceType.GetMethod("RegisterTransformation")?.Invoke(null, new object?[] { payload });

        this._logger.LogInformation("PSE: Registered index.html transformation with File Transformation plugin.");
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
    {
        return
        [
            new TaskTriggerInfo { Type = TaskTriggerInfoType.StartupTrigger },
        ];
    }
}
