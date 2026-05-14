// <copyright file="PasswordPolicyResponse.cs" company="Nicholas Dibb-Fuller">
// Copyright (c) Nicholas Dibb-Fuller. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Jellyfin.Plugin.Template.Api.Models;

using System.Text.Json.Serialization;

/// <summary>
/// The active password strength policy, returned alongside the status check so the client
/// can display accurate requirements and perform matching pre-validation.
/// </summary>
public sealed class PasswordPolicyResponse
{
    /// <summary>Gets or sets the minimum required password length.</summary>
    [JsonPropertyName("minLength")]
    public int MinLength { get; set; }

    /// <summary>Gets or sets a value indicating whether an uppercase letter is required.</summary>
    [JsonPropertyName("requireUppercase")]
    public bool RequireUppercase { get; set; }

    /// <summary>Gets or sets a value indicating whether a lowercase letter is required.</summary>
    [JsonPropertyName("requireLowercase")]
    public bool RequireLowercase { get; set; }

    /// <summary>Gets or sets a value indicating whether a digit is required.</summary>
    [JsonPropertyName("requireDigit")]
    public bool RequireDigit { get; set; }

    /// <summary>Gets or sets a value indicating whether a special character is required.</summary>
    [JsonPropertyName("requireSpecialCharacter")]
    public bool RequireSpecialCharacter { get; set; }
}
