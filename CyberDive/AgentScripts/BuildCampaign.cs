using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
public static class BuildCampaign
{
    public static void Run()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play Mode before building");
        Directory.CreateDirectory("Build/Campaign");
        var report=BuildPipeline.BuildPlayer(new[]{"Assets/CyberDive/Scenes/Dive.unity"},"Build/Campaign/CyberDive.exe",BuildTarget.StandaloneWindows64,BuildOptions.None);
        if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Campaign build failed: "+report.summary.result);
        File.WriteAllText("Build/Campaign/build-result.txt","Succeeded\n"+DateTime.UtcNow.ToString("O"));
    }
}
