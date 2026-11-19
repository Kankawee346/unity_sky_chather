using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;

public static class CatchLabWebBuild
{
    [MenuItem("CatchLab/Build Web Demo")]
    public static void Build()
    {
        if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL, BuildTarget.WebGL))
            throw new InvalidOperationException("Install Web Build Support for this Unity Editor in Unity Hub.");

        string output = Path.GetFullPath("Build/Web");
        PlayerSettings.WebGL.template = "PROJECT:CatchLab";
        PlayerSettings.productName = "CatchLab";
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/SampleScene.unity" },
            locationPathName = output,
            target = BuildTarget.WebGL,
            options = BuildOptions.Development
        });

        if (report.summary.result != BuildResult.Succeeded)
            throw new InvalidOperationException($"CatchLab Web build ended with {report.summary.result}.");

        UnityEngine.Debug.Log($"CatchLab Web built to {output}");
    }
}
