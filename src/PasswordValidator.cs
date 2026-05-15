// <copyright file="PasswordValidator.cs" company="Nicholas Dibb-Fuller">
// Copyright (c) Nicholas Dibb-Fuller. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Jellyfin.Plugin.Template;

using System.Linq;
using Jellyfin.Plugin.Template.Configuration;

/// <summary>
/// Validates password strength against the active plugin policy.
/// </summary>
public static class PasswordValidator
{
    /// <summary>
    /// Validates that a password meets all requirements in <paramref name="config"/>.
    /// </summary>
    /// <param name="password">The plaintext password to validate.</param>
    /// <param name="config">The active plugin configuration containing the policy.</param>
    /// <returns>A <see cref="ValidationResult"/> indicating whether the password is valid.</returns>
    public static ValidationResult Validate(string password, PluginConfiguration config)
    {
        if (string.IsNullOrEmpty(password) || password.Length < config.MinLength)
        {
            return new ValidationResult(false, $"Password must be at least {config.MinLength} characters.");
        }

        if (config.RequireUppercase && !password.Any(char.IsUpper))
        {
            return new ValidationResult(false, "Password must contain at least one uppercase letter.");
        }

        if (config.RequireLowercase && !password.Any(char.IsLower))
        {
            return new ValidationResult(false, "Password must contain at least one lowercase letter.");
        }

        if (config.RequireDigit && !password.Any(char.IsDigit))
        {
            return new ValidationResult(false, "Password must contain at least one number.");
        }

        if (config.RequireSpecialCharacter && !password.Any(c => !char.IsAsciiLetterOrDigit(c)))
        {
            return new ValidationResult(false, "Password must contain at least one special character.");
        }

        return new ValidationResult(true, null);
    }
}
