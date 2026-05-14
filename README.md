# Jellyfin Password Strength Enforcement

A Jellyfin 10.11.8 plugin that enforces strong password policies for all users. Admins can trigger mandatory password resets that immediately log out affected users and block them from using Jellyfin until they set a new password meeting the configured strength requirements.

## Features

- **Configurable password policy** — set minimum length and require uppercase letters, lowercase letters, numbers, and/or special characters
- **Force password reset** — reset all non-admin users, or select specific users, from the plugin config page
- **Admin re-authentication** — any force reset requires the admin to confirm their own password first
- **Post-login enforcement overlay** — flagged users see a blocking modal on their next login that cannot be dismissed without completing a password change
- **Rate limiting** — failed password change attempts are limited to 5 per 15 minutes per user
- **No manual server changes** — installs entirely through the Jellyfin plugin repository GUI

## Installation

1. In Jellyfin, go to **Dashboard → Plugins → Repositories**
2. Add the repository URL (your `manifest.json` raw GitHub URL)
3. Install **Password Strength Enforcement** from the catalogue
4. Install **[File Transformation](https://github.com/IAmParadox27/jellyfin-plugin-file-transformation)** — required for the post-login overlay to work
5. Restart Jellyfin

## Configuration

Open **Dashboard → Plugins → Password Strength Enforcement** to:

- Adjust the password policy (minimum length and character requirements)
- Force a password reset for selected users or all non-admin users

## License

MIT — see [LICENSE](LICENSE).

## Security

See [SECURITY.md](SECURITY.md) for the vulnerability disclosure policy.
