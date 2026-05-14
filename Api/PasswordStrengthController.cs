// <copyright file="PasswordStrengthController.cs" company="npdib ltd">
// Copyright (c) npdib ltd. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Jellyfin.Plugin.Template.Api;

using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
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
    /// Returns whether the authenticated user is required to change their password.
    /// </summary>
    /// <returns>A <see cref="PasswordStatusResponse"/> indicating reset requirement.</returns>
    [HttpGet("Status")]
    [Authorize(Policy = "DefaultAuthorization")]
    public async Task<ActionResult<PasswordStatusResponse>> GetStatusAsync()
    {
        var authInfo = await this._authorizationContext.GetAuthorizationInfo(this.Request).ConfigureAwait(false);
        if (authInfo?.User is null)
        {
            return this.Unauthorized();
        }

        var config = Plugin.Instance!.Configuration;
        var resetRequired = config.ForcedResetUserIds.Contains(
            authInfo.UserId.ToString("N", CultureInfo.InvariantCulture));

        return this.Ok(new PasswordStatusResponse { ResetRequired = resetRequired });
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
    [Authorize(Policy = "DefaultAuthorization")]
    public async Task<ActionResult> ChangePasswordAsync([FromBody] ChangePasswordRequest request)
    {
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
        try
        {
            var remoteIp = this.HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
            await this._userManager.AuthenticateUser(
                authInfo.User.Username,
                request.CurrentPassword,
                string.Empty,
                remoteIp,
                false).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            this._logger.LogWarning(ex, "Current password verification failed for user {UserId}", userId);
            PasswordAttemptTracker.RecordFailure(userId);
            return this.BadRequest(new ErrorResponse("Current password is incorrect."));
        }

        var validation = PasswordValidator.Validate(request.NewPassword);
        if (!validation.IsValid)
        {
            return this.BadRequest(new ErrorResponse(validation.Message!));
        }

        await this._userManager.ChangePassword(authInfo.User, request.NewPassword).ConfigureAwait(false);

        var config = Plugin.Instance!.Configuration;
        config.ForcedResetUserIds.Remove(userId.ToString("N", CultureInfo.InvariantCulture));
        Plugin.Instance.SaveConfiguration();

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
        var authInfo = await this._authorizationContext.GetAuthorizationInfo(this.Request).ConfigureAwait(false);
        if (authInfo?.User is null)
        {
            return this.Unauthorized();
        }

        if (string.IsNullOrEmpty(request.AdminPassword))
        {
            return this.BadRequest(new ErrorResponse("Admin password confirmation is required."));
        }

        // Re-authenticate the admin to confirm the destructive action.
        try
        {
            var remoteIp = this.HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
            await this._userManager.AuthenticateUser(
                authInfo.User.Username,
                request.AdminPassword,
                string.Empty,
                remoteIp,
                false).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            this._logger.LogWarning(
                ex,
                "Admin re-authentication failed for ForceReset — user {AdminUserId}",
                authInfo.UserId);
            return this.Unauthorized(new ErrorResponse("Password confirmation failed."));
        }

        var config = Plugin.Instance!.Configuration;
        config.ForcedResetUserIds.Clear();

        var nonAdminUsers = this._userManager.Users
            .Where(u => !u.HasPermission(PermissionKind.IsAdministrator))
            .ToList();

        foreach (var user in nonAdminUsers)
        {
            config.ForcedResetUserIds.Add(user.Id.ToString("N", CultureInfo.InvariantCulture));

            // Revoke all active sessions so users cannot continue without resetting their password.
            await this._sessionManager.RevokeUserTokens(user.Id, null).ConfigureAwait(false);
        }

        Plugin.Instance.SaveConfiguration();

        this._logger.LogWarning(
            "Force password reset triggered by admin {AdminUserId} — {Count} user(s) affected",
            authInfo.UserId,
            nonAdminUsers.Count);

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

        using var reader = new StreamReader(stream);
        return this.Content(reader.ReadToEnd(), "application/javascript; charset=utf-8");
    }
}
