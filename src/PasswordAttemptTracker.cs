// <copyright file="PasswordAttemptTracker.cs" company="Nicholas Dibb-Fuller">
// Copyright (c) Nicholas Dibb-Fuller. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Jellyfin.Plugin.Template;

using System;
using System.Collections.Generic;

/// <summary>
/// Thread-safe in-memory tracker for failed password change attempts.
/// Enforces a lockout after repeated failures to limit brute-force attacks.
/// </summary>
internal static class PasswordAttemptTracker
{
    private const int MaxFailures = 5;
    private static readonly TimeSpan LockoutWindow = TimeSpan.FromMinutes(15);
    private static readonly object SyncRoot = new object();
    private static readonly Dictionary<Guid, (int Count, DateTime WindowStart)> Attempts =
        new Dictionary<Guid, (int Count, DateTime WindowStart)>();

    /// <summary>
    /// Returns true if the user has exceeded the allowed failure count within the lockout window.
    /// </summary>
    /// <param name="userId">The user's unique identifier.</param>
    /// <returns>True if the user is currently blocked.</returns>
    internal static bool IsBlocked(Guid userId)
    {
        lock (SyncRoot)
        {
            if (!Attempts.TryGetValue(userId, out var entry))
            {
                return false;
            }

            if (DateTime.UtcNow - entry.WindowStart > LockoutWindow)
            {
                Attempts.Remove(userId);
                return false;
            }

            return entry.Count >= MaxFailures;
        }
    }

    /// <summary>
    /// Records a failed attempt for a user, starting a new window if the previous one has expired.
    /// </summary>
    /// <param name="userId">The user's unique identifier.</param>
    internal static void RecordFailure(Guid userId)
    {
        lock (SyncRoot)
        {
            if (Attempts.TryGetValue(userId, out var entry)
                && DateTime.UtcNow - entry.WindowStart <= LockoutWindow)
            {
                Attempts[userId] = (entry.Count + 1, entry.WindowStart);
            }
            else
            {
                Attempts[userId] = (1, DateTime.UtcNow);
            }
        }
    }

    /// <summary>
    /// Clears the failure record for a user after a successful password change.
    /// </summary>
    /// <param name="userId">The user's unique identifier.</param>
    internal static void ClearFailures(Guid userId)
    {
        lock (SyncRoot)
        {
            Attempts.Remove(userId);
        }
    }
}
