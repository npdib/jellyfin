// <copyright file="PolicyUpdateRequest.cs" company="Nicholas Dibb-Fuller">
// Copyright (c) Nicholas Dibb-Fuller. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Jellyfin.Plugin.Template.Api.Models;

/// <summary>
/// Request body for updating the active password policy.
/// </summary>
public sealed class PolicyUpdateRequest
{
    /// <summary>Gets or sets the minimum required password length.</summary>
    public int MinLength { get; set; }

    /// <summary>Gets or sets a value indicating whether an uppercase letter is required.</summary>
    public bool RequireUppercase { get; set; }

    /// <summary>Gets or sets a value indicating whether a lowercase letter is required.</summary>
    public bool RequireLowercase { get; set; }

    /// <summary>Gets or sets a value indicating whether a digit is required.</summary>
    public bool RequireDigit { get; set; }

    /// <summary>Gets or sets a value indicating whether a special character is required.</summary>
    public bool RequireSpecialCharacter { get; set; }
}
