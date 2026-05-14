// <copyright file="PluginConfigResponse.cs" company="Nicholas Dibb-Fuller">
// Copyright (c) Nicholas Dibb-Fuller. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Jellyfin.Plugin.Template.Api.Models;

using System.Text.Json.Serialization;

/// <summary>
/// Response payload for the admin config endpoint, combining the current policy with
/// runtime environment information needed to render the configuration page correctly.
/// </summary>
public sealed class PluginConfigResponse
{
    /// <summary>Gets or sets the active password policy.</summary>
    [JsonPropertyName("policy")]
    public PasswordPolicyResponse Policy { get; set; } = new PasswordPolicyResponse();

    /// <summary>
    /// Gets or sets a value indicating whether the File Transformation plugin was detected.
    /// When false, the client-side enforcement overlay cannot be injected and the config page
    /// should display an installation warning.
    /// </summary>
    [JsonPropertyName("fileTransformationInstalled")]
    public bool FileTransformationInstalled { get; set; }
}
