// <copyright file="PluginServiceRegistrator.cs" company="npdib ltd">
// Copyright (c) npdib ltd. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Jellyfin.Plugin.Template;

using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Registers plugin services into the Jellyfin DI container.
/// </summary>
public sealed class PluginServiceRegistrator : IPluginServiceRegistrator
{
    /// <inheritdoc />
    public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
    {
        serviceCollection.AddSingleton<IScheduledTask, PseStartupTask>();
    }
}
