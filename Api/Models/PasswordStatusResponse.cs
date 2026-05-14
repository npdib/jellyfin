// <copyright file="PasswordStatusResponse.cs" company="npdib ltd">
// Copyright (c) npdib ltd. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Jellyfin.Plugin.Template.Api.Models;

using System.Text.Json.Serialization;

/// <summary>
/// Response from the password status endpoint.
/// </summary>
public sealed class PasswordStatusResponse
{
    /// <summary>
    /// Gets or sets a value indicating whether the authenticated user must change their password.
    /// </summary>
    [JsonPropertyName("resetRequired")]
    public bool ResetRequired { get; set; }
}
