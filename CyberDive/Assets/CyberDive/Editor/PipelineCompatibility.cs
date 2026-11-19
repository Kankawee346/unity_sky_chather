using System.Reflection;
using UnityEditor;
// Pipeline 1.0's OnCodeLoaded publisher calls AssetDatabase before Unity 6.5 allows
// it. Retry the package's own publisher on the Editor main loop; no package edits,
// external endpoints or auth changes. Editor-only; remove after upstream fixes it.
namespace CyberDive.EditorTools
{
    static class PipelineCompatibility
    {
        [InitializeOnLoadMethod]
        static void Schedule()
        {
            EditorApplication.delayCall+=()=>
            {
                foreach(var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
                {
                    var type=assembly.GetType("Unity.Pipeline.PipelineDescriptorPublisher");
                    if(type==null)continue;
                    type.GetMethod("Start",BindingFlags.NonPublic|BindingFlags.Static)?.Invoke(null,null);
                    break;
                }
            };
        }
    }
}
