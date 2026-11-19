using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;

public static class CatchLabFriendWebBuild
{
    [MenuItem("CatchLab Friend/Build Web")]
    public static void Build()
    {
        if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL, BuildTarget.WebGL))
            throw new InvalidOperationException("Install Web Build Support for Unity 6.5 in Unity Hub.");

        PlayerSettings.WebGL.template = "PROJECT:CatchLab";
        PlayerSettings.productName = "CatchLab Friend";
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes = new[] { "Assets/Scenes/SampleScene.unity" },
            locationPathName = Path.GetFullPath("Build/Web"),
            target = BuildTarget.WebGL,
            options = BuildOptions.None
        });
        if (report.summary.result != BuildResult.Succeeded)
            throw new InvalidOperationException("Friend Web build failed: " + report.summary.result);
        UnityEngine.Debug.Log("CatchLab Friend Web build complete.");
    }
}
