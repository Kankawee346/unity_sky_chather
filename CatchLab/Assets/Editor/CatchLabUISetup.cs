using UnityEditor;
using UnityEngine;

public static class CatchLabUISetup
{
    [InitializeOnLoadMethod]
    private static void Initialize()
    {
        EditorApplication.delayCall += () =>
        {
            if (!System.IO.File.Exists("Assets/TextMesh Pro/Resources/TMP Settings.asset"))
                TMPro.TMP_PackageResourceImporter.ImportResources(true, false, false);
        };
    }

    [MenuItem("CatchLab/Preview Knight UI")]
    public static void Preview()
    {
        if (!EditorApplication.isPlaying) EditorApplication.isPlaying = true;
    }
}
