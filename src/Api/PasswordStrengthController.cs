// <copyright file="PasswordStrengthController.cs" company="Nicholas Dibb-Fuller">
// Copyright (c) Nicholas Dibb-Fuller. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Jellyfin.Plugin.Template.Api;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Data;
using Jellyfin.Database.Implementations.Enums;
using Jellyfin.Plugin.Template.Api.Models;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Session;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

/// <summary>
/// API controller for password strength enforcement operations.
/// </summary>
[ApiController]
[Route("PasswordStrength")]
public class PasswordStrengthController : ControllerBase
{
    private readonly IAuthorizationContext _authorizationContext;
    private readonly IUserManager _userManager;
    private readonly ISessionManager _sessionManager;
    private readonly ILogger<PasswordStrengthController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PasswordStrengthController"/> class.
    /// </summary>
    /// <param name="authorizationContext">Jellyfin authorization context.</param>
    /// <param name="userManager">Jellyfin user manager.</param>
    /// <param name="sessionManager">Jellyfin session manager.</param>
    /// <param name="logger">Logger instance.</param>
    public PasswordStrengthController(
        IAuthorizationContext authorizationContext,
        IUserManager userManager,
        ISessionManager sessionManager,
        ILogger<PasswordStrengthController> logger)
    {
        this._authorizationContext = authorizationContext;
        this._userManager = userManager;
        this._sessionManager = sessionManager;
        this._logger = logger;
    }

    /// <summary>
    /// Returns the active password policy and runtime environment information for the admin configuration page.
    /// </summary>
    /// <returns>A <see cref="PluginConfigResponse"/> containing the current policy and plugin status.</returns>
    [HttpGet("Config")]
    [Authorize(Policy = "RequiresElevation")]
    public ActionResult<PluginConfigResponse> GetConfig()
    {
        var instance = Plugin.Instance;
        if (instance is null)
        {
            return this.StatusCode(StatusCodes.Status503ServiceUnavailable, new ErrorResponse("Plugin not ready."));
        }

        this.Response.Headers.CacheControl = "no-store";

        var config = instance.Configuration;
        return this.Ok(new PluginConfigResponse
        {
            FileTransformationInstalled = Plugin.IsFileTransformationAvailable,
            Policy = new PasswordPolicyResponse
            {
                MinLength = config.MinLength,
                RequireUppercase = config.RequireUppercase,
                RequireLowercase = config.RequireLowercase,
                RequireDigit = config.RequireDigit,
                RequireSpecialCharacter = config.RequireSpecialCharacter,
            },
        });
    }

    /// <summary>
    /// Updates the active password policy. Changes take effect immediately for all subsequent password changes.
    /// </summary>
    /// <param name="request">The new policy settings.</param>
    /// <returns>204 No Content on success.</returns>
    [HttpPost("Policy")]
    [Authorize(Policy = "RequiresElevation")]
    public ActionResult UpdatePolicy([FromBody] PolicyUpdateRequest request)
    {
        var instance = Plugin.Instance;
        if (instance is null)
        {
            return this.StatusCode(StatusCodes.Status503ServiceUnavailable, new ErrorResponse("Plugin not ready."));
        }

        if (request.MinLength < 4 || request.MinLength > 128)
        {
            return this.BadRequest(new ErrorResponse("Minimum length must be between 4 and 128."));
        }

        var config = instance.Configuration;
        config.MinLength = request.MinLength;
        config.RequireUppercase = request.RequireUppercase;
        config.RequireLowercase = request.RequireLowercase;
        config.RequireDigit = request.RequireDigit;
        config.RequireSpecialCharacter = request.RequireSpecialCharacter;
        instance.SaveConfiguration();

        this._logger.LogInformation(
            "Password policy updated — MinLength={MinLength} Upper={Upper} Lower={Lower} Digit={Digit} Special={Special}",
            config.MinLength,
            config.RequireUppercase,
            config.RequireLowercase,
            config.RequireDigit,
            config.RequireSpecialCharacter);

        return this.NoContent();
    }

    /// <summary>
    /// Returns whether the authenticated user is required to change their password, plus the active policy.
    /// </summary>
    /// <returns>A <see cref="PasswordStatusResponse"/> indicating reset requirement and current policy.</returns>
    [HttpGet("Status")]
    [Authorize]
    public async Task<ActionResult<PasswordStatusResponse>> GetStatusAsync()
    {
        var instance = Plugin.Instance;
        if (instance is null)
        {
            return this.StatusCode(StatusCodes.Status503ServiceUnavailable, new ErrorResponse("Plugin not ready."));
        }

        var authInfo = await this._authorizationContext.GetAuthorizationInfo(this.Request).ConfigureAwait(false);
        if (authInfo?.User is null)
        {
            return this.Unauthorized();
        }

        this.Response.Headers.CacheControl = "no-store";

        var config = instance.Configuration;
        var resetRequired = Plugin.IsUserFlaggedForReset(
            authInfo.UserId.ToString("N", CultureInfo.InvariantCulture));

        return this.Ok(new PasswordStatusResponse
        {
            ResetRequired = resetRequired,
            Policy = new PasswordPolicyResponse
            {
                MinLength = config.MinLength,
                RequireUppercase = config.RequireUppercase,
                RequireLowercase = config.RequireLowercase,
                RequireDigit = config.RequireDigit,
                RequireSpecialCharacter = config.RequireSpecialCharacter,
            },
        });
    }

    /// <summary>
    /// Changes the authenticated user's password after verifying their current password.
    /// The new password must meet the strength policy.
    /// </summary>
    /// <remarks>
    /// SECURITY: Verifies the current password before accepting any change, protecting against
    /// stolen session tokens being used to hijack accounts. Rate-limited to 5 failures per 15 minutes.
    /// Request bodies are never logged.
    /// </remarks>
    /// <param name="request">The password change request.</param>
    /// <returns>204 No Content on success; 400/401/429 on failure.</returns>
    [HttpPost("ChangePassword")]
    [Authorize]
    public async Task<ActionResult> ChangePasswordAsync([FromBody] ChangePasswordRequest request)
    {
        var instance = Plugin.Instance;
        if (instance is null)
        {
            return this.StatusCode(StatusCodes.Status503ServiceUnavailable, new ErrorResponse("Plugin not ready."));
        }

        var authInfo = await this._authorizationContext.GetAuthorizationInfo(this.Request).ConfigureAwait(false);
        if (authInfo?.User is null)
        {
            return this.Unauthorized();
        }

        if (string.IsNullOrEmpty(request.CurrentPassword) || string.IsNullOrEmpty(request.NewPassword))
        {
            return this.BadRequest(new ErrorResponse("Both current and new passwords are required."));
        }

        var userId = authInfo.UserId;

        if (PasswordAttemptTracker.IsBlocked(userId))
        {
            this._logger.LogWarning("Password change rate limit exceeded for user {UserId}", userId);
            return this.StatusCode(
                StatusCodes.Status429TooManyRequests,
                new ErrorResponse("Too many failed attempts. Please wait 15 minutes before trying again."));
        }

        // Verify current password before accepting the change.
        // This protects against stolen session tokens being used to change a password silently.
        // AuthenticateUser may return null OR throw on failure depending on the Jellyfin version.
        try
        {
            var remoteIp = this.HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
            var verified = await this._userManager.AuthenticateUser(
                authInfo.User.Username,
                request.CurrentPassword,
                remoteIp,
                false).ConfigureAwait(false);

            if (verified is null)
            {
                this._logger.LogWarning("Current password verification returned null for user {UserId}", userId);
                PasswordAttemptTracker.RecordFailure(userId);
                return this.BadRequest(new ErrorResponse("Current password is incorrect."));
            }
        }
        catch (Exception ex)
        {
            this._logger.LogWarning(ex, "Current password verification failed for user {UserId}", userId);
            PasswordAttemptTracker.RecordFailure(userId);
            return this.BadRequest(new ErrorResponse("Current password is incorrect."));
        }

        var config = instance.Configuration;
        var validation = PasswordValidator.Validate(request.NewPassword, config);
        if (!validation.IsValid)
        {
            return this.BadRequest(new ErrorResponse(validation.Message!));
        }

        await this._userManager.ChangePassword(authInfo.User, request.NewPassword).ConfigureAwait(false);

        Plugin.RemoveForcedResetUser(userId.ToString("N", CultureInfo.InvariantCulture));
        instance.SaveConfiguration();

        PasswordAttemptTracker.ClearFailures(userId);
        this._logger.LogInformation("Password changed successfully for user {UserId}", userId);
        return this.NoContent();
    }

    /// <summary>
    /// Flags all non-admin users for a mandatory password reset and revokes their active sessions.
    /// Requires admin role plus password re-confirmation.
    /// </summary>
    /// <remarks>
    /// SECURITY: Re-authenticates the calling admin before acting, preventing misuse of an
    /// already-authenticated admin session (e.g. unattended terminal). All affected sessions are
    /// immediately revoked so users cannot continue without resetting their password.
    /// </remarks>
    /// <param name="request">Admin password for re-authentication.</param>
    /// <returns>204 No Content on success; 401 if re-authentication fails.</returns>
    [HttpPost("ForceReset")]
    [Authorize(Policy = "RequiresElevation")]
    public async Task<ActionResult> ForceResetAsync([FromBody] ForceResetRequest request)
    {
        var instance = Plugin.Instance;
        if (instance is null)
        {
            return this.StatusCode(StatusCodes.Status503ServiceUnavailable, new ErrorResponse("Plugin not ready."));
        }

        var authInfo = await this._authorizationContext.GetAuthorizationInfo(this.Request).ConfigureAwait(false);
        if (authInfo?.User is null)
        {
            return this.Unauthorized();
        }

        if (string.IsNullOrEmpty(request.AdminPassword))
        {
            return this.BadRequest(new ErrorResponse("Admin password confirmation is required."));
        }

        var adminId = authInfo.UserId;

        if (PasswordAttemptTracker.IsBlocked(adminId))
        {
            this._logger.LogWarning("ForceReset rate limit exceeded for admin {AdminUserId}", adminId);
            return this.StatusCode(
                StatusCodes.Status429TooManyRequests,
                new ErrorResponse("Too many failed attempts. Please wait 15 minutes before trying again."));
        }

        // Re-authenticate the admin to confirm the destructive action.
        // AuthenticateUser may return null OR throw on failure depending on the Jellyfin version.
        try
        {
            var remoteIp = this.HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
            var verified = await this._userManager.AuthenticateUser(
                authInfo.User.Username,
                request.AdminPassword,
                remoteIp,
                false).ConfigureAwait(false);

            if (verified is null)
            {
                this._logger.LogWarning(
                    "Admin re-authentication returned null for ForceReset — user {AdminUserId}",
                    adminId);
                PasswordAttemptTracker.RecordFailure(adminId);
                return this.Unauthorized(new ErrorResponse("Password confirmation failed."));
            }
        }
        catch (Exception ex)
        {
            this._logger.LogWarning(
                ex,
                "Admin re-authentication failed for ForceReset — user {AdminUserId}",
                adminId);
            PasswordAttemptTracker.RecordFailure(adminId);
            return this.Unauthorized(new ErrorResponse("Password confirmation failed."));
        }

        PasswordAttemptTracker.ClearFailures(adminId);

        var nonAdminUsers = this._userManager.Users
            .Where(u => !u.HasPermission(PermissionKind.IsAdministrator))
            .ToList();

        Plugin.SetForcedResetUsers(
            nonAdminUsers.Select(u => u.Id.ToString("N", CultureInfo.InvariantCulture)));
        instance.SaveConfiguration();

        foreach (var user in nonAdminUsers)
        {
            // Revoke all active sessions so users cannot continue without resetting their password.
            await this._sessionManager.RevokeUserTokens(user.Id, null).ConfigureAwait(false);
        }

        this._logger.LogWarning(
            "Force password reset triggered by admin {AdminUserId} — {Count} user(s) affected",
            adminId,
            nonAdminUsers.Count);

        return this.NoContent();
    }

    /// <summary>
    /// Flags specific users for a mandatory password reset and revokes their active sessions.
    /// Requires admin role plus password re-confirmation.
    /// </summary>
    /// <remarks>
    /// SECURITY: Re-authenticates the calling admin before acting. Admin users are never affected
    /// regardless of which IDs are submitted. Rate-limited to 5 failures per 15 minutes.
    /// </remarks>
    /// <param name="request">Admin password and list of user IDs to reset.</param>
    /// <returns>204 No Content on success; 400/401/429 on failure.</returns>
    [HttpPost("ForceResetUsers")]
    [Authorize(Policy = "RequiresElevation")]
    public async Task<ActionResult> ForceResetUsersAsync([FromBody] ForceResetUsersRequest request)
    {
        var instance = Plugin.Instance;
        if (instance is null)
        {
            return this.StatusCode(StatusCodes.Status503ServiceUnavailable, new ErrorResponse("Plugin not ready."));
        }

        var authInfo = await this._authorizationContext.GetAuthorizationInfo(this.Request).ConfigureAwait(false);
        if (authInfo?.User is null)
        {
            return this.Unauthorized();
        }

        if (string.IsNullOrEmpty(request.AdminPassword))
        {
            return this.BadRequest(new ErrorResponse("Admin password confirmation is required."));
        }

        if (request.UserIds is null || request.UserIds.Count == 0)
        {
            return this.BadRequest(new ErrorResponse("At least one user ID must be provided."));
        }

        var adminId = authInfo.UserId;

        if (PasswordAttemptTracker.IsBlocked(adminId))
        {
            this._logger.LogWarning("ForceResetUsers rate limit exceeded for admin {AdminUserId}", adminId);
            return this.StatusCode(
                StatusCodes.Status429TooManyRequests,
                new ErrorResponse("Too many failed attempts. Please wait 15 minutes before trying again."));
        }

        try
        {
            var remoteIp = this.HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
            var verified = await this._userManager.AuthenticateUser(
                authInfo.User.Username,
                request.AdminPassword,
                remoteIp,
                false).ConfigureAwait(false);

            if (verified is null)
            {
                this._logger.LogWarning(
                    "Admin re-authentication returned null for ForceResetUsers — user {AdminUserId}",
                    adminId);
                PasswordAttemptTracker.RecordFailure(adminId);
                return this.Unauthorized(new ErrorResponse("Password confirmation failed."));
            }
        }
        catch (Exception ex)
        {
            this._logger.LogWarning(
                ex,
                "Admin re-authentication failed for ForceResetUsers — user {AdminUserId}",
                adminId);
            PasswordAttemptTracker.RecordFailure(adminId);
            return this.Unauthorized(new ErrorResponse("Password confirmation failed."));
        }

        PasswordAttemptTracker.ClearFailures(adminId);

        // Only affect non-admin users regardless of what IDs were submitted.
        var requestedIds = new HashSet<string>(request.UserIds, StringComparer.OrdinalIgnoreCase);
        var targetUsers = this._userManager.Users
            .Where(u => !u.HasPermission(PermissionKind.IsAdministrator)
                        && requestedIds.Contains(u.Id.ToString("N", CultureInfo.InvariantCulture)))
            .ToList();

        foreach (var user in targetUsers)
        {
            Plugin.AddForcedResetUser(user.Id.ToString("N", CultureInfo.InvariantCulture));
        }

        instance.SaveConfiguration();

        foreach (var user in targetUsers)
        {
            await this._sessionManager.RevokeUserTokens(user.Id, null).ConfigureAwait(false);
        }

        this._logger.LogWarning(
            "Targeted password reset triggered by admin {AdminUserId} — {Count} user(s) affected",
            adminId,
            targetUsers.Count);

        return this.NoContent();
    }

    /// <summary>
    /// Serves the client-side enforcement script as a static JavaScript file.
    /// This endpoint is intentionally anonymous — the script contains no sensitive data.
    /// </summary>
    /// <returns>The enforcement JavaScript file.</returns>
    [HttpGet("enforcement.js")]
    [AllowAnonymous]
    public IActionResult GetEnforcementScript()
    {
        var assembly = this.GetType().Assembly;
        using var stream = assembly.GetManifestResourceStream("Jellyfin.Plugin.Template.web.enforcement.js");

        if (stream is null)
        {
            return this.NotFound();
        }

        this.Response.Headers.CacheControl = "public, max-age=3600";
        using var reader = new StreamReader(stream);
        return this.Content(reader.ReadToEnd(), "application/javascript; charset=utf-8");
    }
}
