using UnityEngine;
namespace CyberDive
{
    public static class EnergyArt
    {
        static Material material;
        public static Material Material=>material?material:material=new Material(Resources.Load<Shader>("Energy"));
        public static LineRenderer Trace(Transform parent,int points,float width)
        {
            var obj=new GameObject("EnergyTrace");obj.transform.SetParent(parent,false);var line=obj.AddComponent<LineRenderer>();
            line.useWorldSpace=false;line.sharedMaterial=Material;line.positionCount=points;line.widthMultiplier=width;
            line.numCapVertices=2;line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;return line;
        }
        public static void Circle(LineRenderer line,float radius)
        {
            for(int i=0;i<line.positionCount;i++){float a=i*Mathf.PI*2/(line.positionCount-1);line.SetPosition(i,new Vector3(Mathf.Cos(a)*radius,Mathf.Sin(a)*radius,0));}
        }
    }
}
