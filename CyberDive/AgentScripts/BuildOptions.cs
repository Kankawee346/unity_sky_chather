using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
public static class BuildOptions
{
    public static void Run()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play Mode before building");
        Directory.CreateDirectory("Build/Options");
        var report=BuildPipeline.BuildPlayer(new[]{"Assets/CyberDive/Scenes/Dive.unity"},"Build/Options/CyberDive.exe",BuildTarget.StandaloneWindows64,UnityEditor.BuildOptions.None);
        if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Build failed: "+report.summary.result);
        File.WriteAllText("Build/Options/build-result.txt","Succeeded\n"+DateTime.UtcNow.ToString("O"));
    }
}
