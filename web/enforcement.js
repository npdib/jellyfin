/**
 * Password Strength Enforcement — client-side overlay.
 * Loaded by Jellyfin's custom.js and runs on every page in the web UI.
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

    // ── Helpers ──────────────────────────────────────────────────────────────

    function getToken() {
        if (typeof ApiClient !== 'undefined' && ApiClient.accessToken) {
            return ApiClient.accessToken();
        }

        return null;
    }

    function isLoggedIn() {
        if (typeof ApiClient === 'undefined') {
            return false;
        }

        return typeof ApiClient.isLoggedIn === 'function'
            ? ApiClient.isLoggedIn()
            : !!getToken();
    }

    function buildUrl(path) {
        if (typeof ApiClient !== 'undefined' && ApiClient.getUrl) {
            return ApiClient.getUrl(path);
        }

        return path;
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
                            showModal();
                        }
                    });
                }
            })
            .catch(function () {
                isChecking = false;
            });
    }

    // ── Modal ─────────────────────────────────────────────────────────────────

    function showModal() {
        if (document.getElementById(MODAL_ID)) {
            return;
        }

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
            'display:flex',
            'align-items:center',
            'justify-content:center',
            'font-family:inherit',
        ].join(';');

        var card = document.createElement('div');
        card.style.cssText = [
            'background:#1c1c1c',
            'color:#e0e0e0',
            'padding:2rem',
            'border-radius:8px',
            'width:90%',
            'max-width:380px',
            'box-sizing:border-box',
            'box-shadow:0 8px 32px rgba(0,0,0,0.6)',
        ].join(';');

        card.innerHTML = [
            '<h2 style="margin:0 0 0.4rem;font-size:1.25rem">Password Change Required</h2>',
            '<p style="margin:0 0 1.25rem;color:#999;font-size:0.875rem;line-height:1.5">',
            'Your password must be updated before you can continue.<br>',
            'Requirements: at least 8 characters, one uppercase letter, one lowercase letter, one number.',
            '</p>',
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

        overlay.appendChild(card);
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

        // Client-side pre-check for UX feedback only.
        // The server performs the authoritative validation.
        if (newPwd.length < 8
            || !/[A-Z]/.test(newPwd)
            || !/[a-z]/.test(newPwd)
            || !/[0-9]/.test(newPwd)) {
            showError('Password must be at least 8 characters with an uppercase letter, a lowercase letter, and a number.');
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
