// <copyright file="PluginConfiguration.cs" company="Nicholas Dibb-Fuller">
// Copyright (c) Nicholas Dibb-Fuller. All rights reserved.
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

    /// <summary>Gets or sets the minimum required password length.</summary>
    public int MinLength { get; set; } = 8;

    /// <summary>Gets or sets a value indicating whether an uppercase letter is required.</summary>
    public bool RequireUppercase { get; set; } = true;

    /// <summary>Gets or sets a value indicating whether a lowercase letter is required.</summary>
    public bool RequireLowercase { get; set; } = true;

    /// <summary>Gets or sets a value indicating whether a digit is required.</summary>
    public bool RequireDigit { get; set; } = true;

    /// <summary>Gets or sets a value indicating whether a special character is required.</summary>
    public bool RequireSpecialCharacter { get; set; }
}
