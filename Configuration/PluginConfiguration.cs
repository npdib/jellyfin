// <copyright file="PluginConfiguration.cs" company="npdib ltd">
// Copyright (c) npdib ltd. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Jellyfin.Plugin.Template.Configuration;

using System.Collections.Generic;
using MediaBrowser.Model.Plugins;

/// <summary>
/// Plugin configuration.
/// </summary>
public class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PluginConfiguration"/> class.
    /// </summary>
    public PluginConfiguration()
    {
        this.ForcedResetUserIds = new List<string>();
    }

    /// <summary>
    /// Gets or sets the list of user IDs (format N, no hyphens) that must reset their password on next login.
    /// </summary>
    public List<string> ForcedResetUserIds { get; set; }
}
