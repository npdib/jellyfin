/**
 * Password Strength Enforcement — client-side overlay.
 * Injected into index.html by the File Transformation plugin and runs on every page.
 *
 * Security notes:
 *  - All validation rules are ALSO enforced server-side; this is UX only.
 *  - Passwords are sent only over the existing Jellyfin API connection (HTTPS required).
 *  - The modal cannot be dismissed without a successful password change.
 */
(function () {
    'use strict';

    var CHANGE_URL = '/PasswordStrength/ChangePassword';
    var STATUS_URL = '/PasswordStrength/Status';
    var MODAL_ID = 'pse-modal';

    var isChecking = false;
    var isModalVisible = false;
    var currentPolicy = null;

    // ── Helpers ──────────────────────────────────────────────────────────────

    function getToken() {
        // Try ApiClient first (works in older Jellyfin web builds)
        if (typeof ApiClient !== 'undefined' && typeof ApiClient.accessToken === 'function') {
            var t = ApiClient.accessToken();
            if (t) {
                return t;
            }
        }

        // Fallback: read directly from localStorage where Jellyfin always persists the token
        try {
            var raw = localStorage.getItem('jellyfin_credentials');
            if (raw) {
                var creds = JSON.parse(raw);
                var servers = creds.Servers || creds.servers || [];
                for (var i = 0; i < servers.length; i++) {
                    var token = servers[i].AccessToken || servers[i].accessToken;
                    if (token) {
                        return token;
                    }
                }
            }
        } catch (e) { /* storage unavailable */ }

        return null;
    }

    function isLoggedIn() {
        return !!getToken();
    }

    function buildUrl(path) {
        if (typeof ApiClient !== 'undefined' && ApiClient.getUrl) {
            return ApiClient.getUrl(path);
        }

        return path;
    }

    // ── Policy helpers ────────────────────────────────────────────────────────

    var DEFAULT_POLICY = {
        minLength: 8,
        requireUppercase: true,
        requireLowercase: true,
        requireDigit: true,
        requireSpecialCharacter: false,
    };

    function buildRequirementsText(policy) {
        var minLen = parseInt(policy.minLength, 10) || 8;
        var items = ['At least ' + minLen + ' characters'];
        if (policy.requireUppercase) { items.push('At least one uppercase letter'); }
        if (policy.requireLowercase) { items.push('At least one lowercase letter'); }
        if (policy.requireDigit) { items.push('At least one number'); }
        if (policy.requireSpecialCharacter) { items.push('At least one special character'); }
        return '<ul style="margin:0.5rem 0 0;padding-left:1.25rem">'
            + items.map(function (i) { return '<li>' + i + '</li>'; }).join('')
            + '</ul>';
    }

    function validatePolicy(pwd, policy) {
        var minLen = parseInt(policy.minLength, 10) || 8;
        if (pwd.length < minLen) {
            return 'Password must be at least ' + minLen + ' characters.';
        }
        if (policy.requireUppercase && !/[A-Z]/.test(pwd)) {
            return 'Password must contain at least one uppercase letter.';
        }
        if (policy.requireLowercase && !/[a-z]/.test(pwd)) {
            return 'Password must contain at least one lowercase letter.';
        }
        if (policy.requireDigit && !/[0-9]/.test(pwd)) {
            return 'Password must contain at least one number.';
        }
        if (policy.requireSpecialCharacter && !/[^A-Za-z0-9]/.test(pwd)) {
            return 'Password must contain at least one special character.';
        }
        return null;
    }

    // ── Status check ─────────────────────────────────────────────────────────

    function checkStatus() {
        if (isChecking || isModalVisible || !isLoggedIn()) {
            return;
        }

        var token = getToken();
        if (!token) {
            return;
        }

        isChecking = true;

        window.fetch(buildUrl(STATUS_URL), {
            headers: {
                'Accept': 'application/json',
                'X-Emby-Token': token,
            },
        })
            .then(function (r) {
                isChecking = false;
                if (r.ok) {
                    return r.json().then(function (data) {
                        if (data.resetRequired) {
                            showModal(data.policy || DEFAULT_POLICY);
                        }
                    });
                }
            })
            .catch(function () {
                isChecking = false;
            });
    }

    // ── Modal ─────────────────────────────────────────────────────────────────

    function showModal(policy) {
        if (document.getElementById(MODAL_ID)) {
            return;
        }

        currentPolicy = policy || DEFAULT_POLICY;
        isModalVisible = true;

        var overlay = document.createElement('div');
        overlay.id = MODAL_ID;
        overlay.setAttribute('role', 'dialog');
        overlay.setAttribute('aria-modal', 'true');
        overlay.setAttribute('aria-label', 'Password change required');
        overlay.style.cssText = [
            'position:fixed',
            'inset:0',
            'background:rgba(0,0,0,0.92)',
            'z-index:99999',
            'overflow:hidden',
            'font-family:inherit',
        ].join(';');

        // position:absolute (not fixed) is required — Android Chrome does not
        // allow touch-scrolling a position:fixed overflow container.
        // padding-top centres the card visually; padding-bottom ensures the
        // content is always taller than the viewport so the user can scroll
        // the card up out of the way when the keyboard opens.
        var scroller = document.createElement('div');
        scroller.style.cssText = [
            'position:absolute',
            'inset:0',
            'overflow-y:scroll',
            'overscroll-behavior:contain',
            'padding:20vh 1rem 50vh',
            'box-sizing:border-box',
        ].join(';');

        var card = document.createElement('div');
        card.style.cssText = [
            'background:#1c1c1c',
            'color:#e0e0e0',
            'padding:2rem',
            'border-radius:8px',
            'width:100%',
            'max-width:380px',
            'box-sizing:border-box',
            'box-shadow:0 8px 32px rgba(0,0,0,0.6)',
            'margin:0 auto',
        ].join(';');

        card.innerHTML = [
            '<h2 style="margin:0 0 0.4rem;font-size:1.25rem">Password Change Required</h2>',
            '<div style="margin:0 0 1.25rem;color:#999;font-size:0.875rem;line-height:1.5">',
            '<p style="margin:0 0 0.25rem">Your password must be updated before you can continue. If your previous password fit the specified requirements, just enter the same password again.</p>',
            buildRequirementsText(currentPolicy),
            '</div>',
            '<div id="pse-error" style="display:none;color:#ff6b6b;font-size:0.875rem;margin-bottom:0.75rem"></div>',
            buildField('pse-current', 'Current Password', 'current-password'),
            buildField('pse-new', 'New Password', 'new-password'),
            buildField('pse-confirm', 'Confirm New Password', 'new-password'),
            '<button id="pse-submit" style="',
            'width:100%;padding:0.75rem;margin-top:0.5rem;',
            'background:#00a4dc;color:#fff;border:none;border-radius:4px;',
            'cursor:pointer;font-size:1rem;font-weight:600',
            '">Change Password</button>',
        ].join('');

        scroller.appendChild(card);
        overlay.appendChild(scroller);
        document.body.appendChild(overlay);

        document.getElementById('pse-current').focus();
        document.getElementById('pse-submit').addEventListener('click', handleSubmit);
        document.addEventListener('keydown', trapEscape, true);
    }

    function buildField(id, label, autocomplete) {
        return [
            '<div style="margin-bottom:0.75rem">',
            '<label for="' + id + '" style="display:block;margin-bottom:0.3rem;font-size:0.8rem;color:#aaa">' + label + '</label>',
            '<input id="' + id + '" type="password" autocomplete="' + autocomplete + '"',
            ' style="width:100%;padding:0.5rem 0.6rem;background:#2b2b2b;border:1px solid #444;',
            'border-radius:4px;color:#e0e0e0;font-size:0.95rem;box-sizing:border-box">',
            '</div>',
        ].join('');
    }

    function trapEscape(e) {
        if (!document.getElementById(MODAL_ID)) {
            document.removeEventListener('keydown', trapEscape, true);
            return;
        }

        // Block Escape so the user cannot dismiss the modal
        if (e.key === 'Escape') {
            e.stopPropagation();
            e.preventDefault();
        }
    }

    function showError(msg) {
        var el = document.getElementById('pse-error');
        if (el) {
            el.textContent = msg;
            el.style.display = 'block';
        }
    }

    function clearError() {
        var el = document.getElementById('pse-error');
        if (el) {
            el.style.display = 'none';
        }
    }

    // ── Submit handler ────────────────────────────────────────────────────────

    function handleSubmit() {
        clearError();

        var current = document.getElementById('pse-current').value;
        var newPwd = document.getElementById('pse-new').value;
        var confirm = document.getElementById('pse-confirm').value;

        if (!current || !newPwd || !confirm) {
            showError('Please fill in all fields.');
            return;
        }

        if (newPwd !== confirm) {
            showError('New passwords do not match.');
            return;
        }

        // Client-side pre-check for UX feedback only — server performs authoritative validation.
        var policyError = validatePolicy(newPwd, currentPolicy || DEFAULT_POLICY);
        if (policyError) {
            showError(policyError);
            return;
        }

        var btn = document.getElementById('pse-submit');
        btn.disabled = true;
        btn.textContent = 'Changing…';

        var token = getToken();

        window.fetch(buildUrl(CHANGE_URL), {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Accept': 'application/json',
                'X-Emby-Token': token,
            },
            body: JSON.stringify({
                currentPassword: current,
                newPassword: newPwd,
            }),
        })
            .then(function (r) {
                if (r.status === 204) {
                    var modal = document.getElementById(MODAL_ID);
                    if (modal) {
                        modal.remove();
                    }

                    document.removeEventListener('keydown', trapEscape, true);
                    isModalVisible = false;
                    return;
                }

                return r.json().then(function (data) {
                    showError(data.message || 'An error occurred. Please try again.');
                    btn.disabled = false;
                    btn.textContent = 'Change Password';
                });
            })
            .catch(function () {
                showError('A network error occurred. Please try again.');
                btn.disabled = false;
                btn.textContent = 'Change Password';
            });
    }

    // ── Initialisation ────────────────────────────────────────────────────────

    function init() {
        // Check on every page/view transition within the Jellyfin SPA
        document.addEventListener('viewshow', checkStatus);

        // Check when the user logs in or switches
        if (typeof Events !== 'undefined') {
            Events.on(window, 'userswitched', checkStatus);
        }

        // Poll until we detect a logged-in session, then check once and stop.
        // This catches login events that don't surface via viewshow/userswitched.
        var pollCount = 0;
        var poll = setInterval(function () {
            pollCount += 1;
            if (pollCount > 300) {
                clearInterval(poll);
                return;
            }
            if (isLoggedIn()) {
                clearInterval(poll);
                checkStatus();
            }
        }, 1000);
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        window.setTimeout(init, 500);
    }
}());
