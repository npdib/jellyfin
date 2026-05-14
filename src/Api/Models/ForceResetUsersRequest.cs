// <copyright file="ForceResetUsersRequest.cs" company="Nicholas Dibb-Fuller">
// Copyright (c) Nicholas Dibb-Fuller. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Jellyfin.Plugin.Template.Api.Models;

using System.Collections.Generic;

/// <summary>
/// Request body for the targeted user force-reset endpoint.
/// Intentionally a class (not a record) to prevent auto-generated ToString() from exposing the password.
/// </summary>
public sealed class ForceResetUsersRequest
{
    /// <summary>
    /// Gets or sets the admin's current password, used to re-authenticate before triggering the reset.
    /// </summary>
    public string AdminPassword { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the list of user IDs (format N, no hyphens) to flag for a password reset.
    /// </summary>
    public List<string> UserIds { get; set; } = new List<string>();
}
