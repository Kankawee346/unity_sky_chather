using System;
using System.Reflection;
using UnityEditor;
public static class PreviewResolution
{
    public static void Run()
    {
        var assembly=typeof(EditorWindow).Assembly;
        var flags=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;
        var sizesType=assembly.GetType("UnityEditor.GameViewSizes");
        var singleton=typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
        var sizes=singleton.GetProperty("instance",BindingFlags.Static|BindingFlags.Public).GetValue(null);
        var groupEnum=assembly.GetType("UnityEditor.GameViewSizeGroupType");
        var group=sizesType.GetMethod("GetGroup").Invoke(sizes,new[]{Enum.Parse(groupEnum,"Standalone")});
        var sizeType=assembly.GetType("UnityEditor.GameViewSize");
        var kind=assembly.GetType("UnityEditor.GameViewSizeType");
        var size=Activator.CreateInstance(sizeType,flags,null,new object[]{Enum.Parse(kind,"FixedResolution"),1920,1080,"CYBER DIVE 1080p"},null);
        group.GetType().GetMethod("AddCustomSize").Invoke(group,new[]{size});
        int count=(int)group.GetType().GetMethod("GetTotalCount").Invoke(group,null);
        var viewType=assembly.GetType("UnityEditor.GameView");
        var view=EditorWindow.GetWindow(viewType);
        viewType.GetProperty("selectedSizeIndex",flags).SetValue(view,count-1);
        view.Repaint();
    }
}
