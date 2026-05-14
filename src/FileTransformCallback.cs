// <copyright file="FileTransformCallback.cs" company="Nicholas Dibb-Fuller">
// Copyright (c) Nicholas Dibb-Fuller. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Jellyfin.Plugin.Template;

using System;
using Newtonsoft.Json.Linq;

/// <summary>
/// Callback invoked by the File Transformation plugin to inject the enforcement loader into index.html.
/// </summary>
public static class FileTransformCallback
{
    // Injected before </body> — uses window.location.origin so it works behind any reverse proxy.
    private const string Injection =
        "<script>" +
        "(function(){" +
        "var s=document.createElement('script');" +
        "s.src=(window.location.origin||'')+'/PasswordStrength/enforcement.js';" +
        "s.defer=true;" +
        "document.head.appendChild(s);" +
        "}());" +
        "</script>";

    /// <summary>
    /// Receives the contents of index.html and injects the PSE loader script before the closing body tag.
    /// Called via reflection by the File Transformation plugin.
    /// </summary>
    /// <param name="payload">JSON object with a "contents" field containing the file text.</param>
    /// <returns>Transformed file contents.</returns>
    public static string InjectLoader(JObject payload)
    {
        var contents = payload["contents"]?.ToString() ?? string.Empty;

        // Guard: only transform HTML. The File Transformation plugin may match chunk .js files
        // whose names contain "index" + any char + "html" if the pattern is treated as a regex.
        if (!contents.TrimStart().StartsWith('<'))
        {
            return contents;
        }

        var idx = contents.LastIndexOf("</body>", StringComparison.OrdinalIgnoreCase);
        if (idx >= 0)
        {
            return string.Concat(contents.AsSpan(0, idx), Injection, contents.AsSpan(idx));
        }

        return contents + Injection;
    }
}
