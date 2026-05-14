# Security Policy

## Supported Versions

| Version | Supported |
|---------|-----------|
| Latest  | Yes       |

Only the latest released version receives security fixes. Please update before reporting.

## Reporting a Vulnerability

Please **do not** open a public GitHub issue for security vulnerabilities.

Report vulnerabilities by email to **npdib@proton.me** with:

- A description of the vulnerability
- Steps to reproduce
- Potential impact
- Any suggested mitigations

You can expect an acknowledgement within 48 hours and a fix or mitigation plan within 14 days for confirmed issues.

## Security Design Notes

- All password validation is enforced **server-side**; the client-side overlay is UX only
- Password change requires verification of the current password before accepting a new one, protecting against session token theft
- Force reset requires admin re-authentication to prevent misuse of an unattended authenticated session
- Failed password change attempts are rate-limited to 5 per 15 minutes per user
- Request bodies containing passwords are never logged
- The enforcement script (`/PasswordStrength/enforcement.js`) contains no sensitive data and is served anonymously
