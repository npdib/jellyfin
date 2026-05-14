// <copyright file="PasswordValidator.cs" company="npdib ltd">
// Copyright (c) npdib ltd. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Jellyfin.Plugin.Template;

using System.Linq;

/// <summary>
/// Validates password strength against the enforcement policy.
/// </summary>
public static class PasswordValidator
{
    private const int MinLength = 8;

    /// <summary>
    /// Validates that a password meets the minimum strength requirements.
    /// </summary>
    /// <param name="password">The plaintext password to validate.</param>
    /// <returns>A <see cref="ValidationResult"/> indicating whether the password is valid.</returns>
    public static ValidationResult Validate(string password)
    {
        if (string.IsNullOrEmpty(password) || password.Length < MinLength)
        {
            return new ValidationResult(false, $"Password must be at least {MinLength} characters.");
        }

        if (!password.Any(char.IsUpper))
        {
            return new ValidationResult(false, "Password must contain at least one uppercase letter.");
        }

        if (!password.Any(char.IsLower))
        {
            return new ValidationResult(false, "Password must contain at least one lowercase letter.");
        }

        if (!password.Any(char.IsDigit))
        {
            return new ValidationResult(false, "Password must contain at least one number.");
        }

        return new ValidationResult(true, null);
    }
}
