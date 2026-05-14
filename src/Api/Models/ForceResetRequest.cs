// <copyright file="ForceResetRequest.cs" company="Nicholas Dibb-Fuller">
// Copyright (c) Nicholas Dibb-Fuller. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Jellyfin.Plugin.Template.Api.Models;

/// <summary>
/// Request body for the admin force-reset endpoint.
/// Intentionally a class (not a record) to prevent auto-generated ToString() from exposing the password.
/// </summary>
public sealed class ForceResetRequest
{
    /// <summary>
    /// Gets or sets the admin's current password, used to re-authenticate before triggering the reset.
    /// </summary>
    public string AdminPassword { get; set; } = string.Empty;
}
