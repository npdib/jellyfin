// <copyright file="ValidationResult.cs" company="npdib ltd">
// Copyright (c) npdib ltd. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Jellyfin.Plugin.Template;

/// <summary>
/// Represents the outcome of a password strength validation.
/// </summary>
public sealed class ValidationResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationResult"/> class.
    /// </summary>
    /// <param name="isValid">Whether the password passed validation.</param>
    /// <param name="message">The failure message, or null on success.</param>
    public ValidationResult(bool isValid, string? message)
    {
        this.IsValid = isValid;
        this.Message = message;
    }

    /// <summary>
    /// Gets a value indicating whether the password passed all rules.
    /// </summary>
    public bool IsValid { get; }

    /// <summary>
    /// Gets the human-readable failure reason, or null if validation passed.
    /// </summary>
    public string? Message { get; }
}
