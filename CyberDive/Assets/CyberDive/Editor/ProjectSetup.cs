using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace CyberDive.EditorTools
{
    public static class ProjectSetup
    {
        [MenuItem("CYBER DIVE/Create or Reset Dive Scene")]
        public static void CreateScene()
        {
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            new GameObject("CYBER DIVE").AddComponent<DiveGame>();
            Directory.CreateDirectory("Assets/CyberDive/Scenes");
            var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/PC_RPAsset.asset");
            if(pipeline){GraphicsSettings.defaultRenderPipeline=pipeline;QualitySettings.renderPipeline=pipeline;}
            PlayerSettings.productName="CYBER DIVE";PlayerSettings.companyName="CyberDive";PlayerSettings.runInBackground=true;
            PlayerSettings.defaultScreenWidth=1920;PlayerSettings.defaultScreenHeight=1080;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;
            EditorSceneManager.SaveScene(scene,"Assets/CyberDive/Scenes/Dive.unity");
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/CyberDive/Scenes/Dive.unity",true)};
            AssetDatabase.SaveAssets();Debug.Log("CYBER DIVE scene saved.");
        }
    }
    public static class BuildPlayer
    {
        [MenuItem("CYBER DIVE/Build Windows Player")]
        public static void Build()
        {
            const string effect="Assets/CyberDive/Art/Resources/ParryBurst.vfx";
            if(!File.Exists(effect))
            {
                if(!AssetDatabase.CopyAsset("Packages/com.unity.visualeffectgraph/Editor/Templates/Simple_Burst.vfx",effect))throw new System.Exception("VFX template copy failed");
                AssetDatabase.ImportAsset(effect,ImportAssetOptions.ForceSynchronousImport);
            }
            Directory.CreateDirectory("Build/Windows");
            var report=BuildPipeline.BuildPlayer(new[]{"Assets/CyberDive/Scenes/Dive.unity"},"Build/Windows/CyberDive.exe",BuildTarget.StandaloneWindows64,BuildOptions.None);
            if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new System.Exception("Build failed: "+report.summary.result);
            Debug.Log("CYBER DIVE Windows build succeeded.");
        }
    }
}
