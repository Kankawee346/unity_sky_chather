using System;
using UnityEngine;

internal static class BridgeEndpoint
{
    public static string Resolve(string path)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        return Uri.TryCreate(Application.absoluteURL, UriKind.Absolute, out Uri page)
            ? new Uri(page, path).AbsoluteUri
            : null;
#else
        return "http://127.0.0.1:8787" + path;
#endif
    }
}
