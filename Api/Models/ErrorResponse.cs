// <copyright file="ErrorResponse.cs" company="npdib ltd">
// Copyright (c) npdib ltd. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Jellyfin.Plugin.Template.Api.Models;

using System.Text.Json.Serialization;

/// <summary>
/// A simple error response body containing a human-readable message.
/// </summary>
public sealed class ErrorResponse
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ErrorResponse"/> class.
    /// </summary>
    /// <param name="message">The error message to return to the client.</param>
    public ErrorResponse(string message)
    {
        this.Message = message;
    }

    /// <summary>
    /// Gets the error message.
    /// </summary>
    [JsonPropertyName("message")]
    public string Message { get; }
}
