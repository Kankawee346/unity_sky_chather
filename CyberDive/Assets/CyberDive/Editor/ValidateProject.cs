using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
namespace CyberDive.EditorTools
{
    [InitializeOnLoad]
    public static class ValidateProject
    {
        static TestRunnerApi api;
        static ValidateProject()
        {
            EditorApplication.delayCall+=()=>{api=ScriptableObject.CreateInstance<TestRunnerApi>();api.RegisterCallbacks(new Results());};
        }
        [MenuItem("CYBER DIVE/Run Gameplay Tests")]
        public static void Run()
        {
            api.Execute(new ExecutionSettings(new Filter{testMode=TestMode.PlayMode,assemblyNames=new[]{"CyberDive.PlayTests"}}));
        }
        [MenuItem("CYBER DIVE/Run Rule Tests")]
        public static void RunRules()
        {
            api.Execute(new ExecutionSettings(new Filter{testMode=TestMode.EditMode,assemblyNames=new[]{"CyberDive.Tests"}}));
        }
        class Results : ICallbacks
        {
            public void RunStarted(ITestAdaptor t){}
            public void TestStarted(ITestAdaptor t){}
            public void TestFinished(ITestResultAdaptor r){}
            public void RunFinished(ITestResultAdaptor r)
            {
                TestRunnerApi.SaveResultToFile(r,"../docs/superpowers/plans/cyber-dive-latest-tests.xml");
                Debug.Log($"CYBER TESTS: {r.PassCount} passed, {r.FailCount} failed");
            }
        }
    }
}
