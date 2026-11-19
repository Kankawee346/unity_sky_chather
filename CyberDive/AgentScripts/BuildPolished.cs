using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
public static class BuildPolished
{
    public static void Run()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play Mode before building");
        Directory.CreateDirectory("Build/Polished");
        var report=BuildPipeline.BuildPlayer(new[]{"Assets/CyberDive/Scenes/Dive.unity"},"Build/Polished/CyberDive.exe",BuildTarget.StandaloneWindows64,BuildOptions.None);
        if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Build failed: "+report.summary.result);
        File.WriteAllText("Build/Polished/build-result.txt","Succeeded\n"+DateTime.UtcNow.ToString("O"));
    }
}
