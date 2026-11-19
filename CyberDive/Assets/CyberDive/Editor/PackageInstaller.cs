using System;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
namespace CyberDive.EditorTools
{
    public static class PackageInstaller
    {
        static AddAndRemoveRequest request;
        static double deadline;
        public static void Install()
        {
            request=Client.AddAndRemove(new[]{"com.unity.cinemachine","com.unity.probuilder","com.unity.visualeffectgraph"});
            deadline=EditorApplication.timeSinceStartup+600;
            EditorApplication.update+=Poll;
        }
        static void Poll()
        {
            if(!request.IsCompleted && EditorApplication.timeSinceStartup<deadline)return;
            EditorApplication.update-=Poll;
            if(request.IsCompleted && request.Status==StatusCode.Success)EditorApplication.Exit(0);
            else {UnityEngine.Debug.LogError(request.Error?.message??"Package install timeout");EditorApplication.Exit(1);}
        }
    }
}
