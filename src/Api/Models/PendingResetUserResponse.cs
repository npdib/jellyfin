// <copyright file="PendingResetUserResponse.cs" company="Nicholas Dibb-Fuller">
// Copyright (c) Nicholas Dibb-Fuller. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Jellyfin.Plugin.Template.Api.Models;

/// <summary>
/// A user who is currently flagged for a mandatory password reset.
/// </summary>
public class PendingResetUserResponse
{
    /// <summary>Gets or sets the user ID (format N, no hyphens).</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Gets or sets the display name of the user.</summary>
    public string Name { get; set; } = string.Empty;
}
