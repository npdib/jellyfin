// <copyright file="UserCreatedHandler.cs" company="Nicholas Dibb-Fuller">
// Copyright (c) Nicholas Dibb-Fuller. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Jellyfin.Plugin.Template;

using System.Globalization;
using System.Threading.Tasks;
using Jellyfin.Data;
using Jellyfin.Data.Events.Users;
using Jellyfin.Database.Implementations.Enums;
using MediaBrowser.Controller.Events;
using Microsoft.Extensions.Logging;

/// <summary>
/// Flags newly created non-admin users for a mandatory password reset.
/// </summary>
public class UserCreatedHandler : IEventConsumer<UserCreatedEventArgs>
{
    private readonly ILogger<UserCreatedHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserCreatedHandler"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    public UserCreatedHandler(ILogger<UserCreatedHandler> logger)
    {
        this._logger = logger;
    }

    /// <inheritdoc />
    public Task OnEvent(UserCreatedEventArgs eventArgs)
    {
        var user = eventArgs.Argument;

        if (user.HasPermission(PermissionKind.IsAdministrator))
        {
            return Task.CompletedTask;
        }

        var userIdN = user.Id.ToString("N", CultureInfo.InvariantCulture);
        Plugin.AddForcedResetUser(userIdN);
        Plugin.Instance?.SaveConfiguration();

        this._logger.LogInformation(
            "New user {UserId} ({Username}) auto-flagged for a mandatory password reset.",
            userIdN,
            user.Username);

        return Task.CompletedTask;
    }
}
