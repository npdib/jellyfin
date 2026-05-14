// <copyright file="ChangePasswordRequest.cs" company="Nicholas Dibb-Fuller">
// Copyright (c) Nicholas Dibb-Fuller. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Jellyfin.Plugin.Template.Api.Models;

/// <summary>
/// Request body for the password change endpoint.
/// Intentionally a class (not a record) to prevent auto-generated ToString() from exposing passwords.
/// </summary>
public sealed class ChangePasswordRequest
{
    /// <summary>
    /// Gets or sets the user's current password for verification.
    /// </summary>
    public string CurrentPassword { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the new password to set.
    /// </summary>
    public string NewPassword { get; set; } = string.Empty;
}
