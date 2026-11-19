using UnityEngine;
namespace CyberDive
{
    public sealed class HudRing : UnityEngine.UI.MaskableGraphic
    {
        float fill=1;
        public float Thickness=6;
        public float Fill {get=>fill;set{float next=Mathf.Clamp01(value);if(Mathf.Approximately(fill,next))return;fill=next;SetVerticesDirty();}}
        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper mesh)
        {
            mesh.Clear();float outer=Mathf.Min(rectTransform.rect.width,rectTransform.rect.height)*.5f;
            float inner=Mathf.Max(0,outer-Thickness);var center=rectTransform.rect.center;
            for(int i=0;i<Mathf.CeilToInt(96*fill);i++)
            {
                float a=(90-i*360f/96)*Mathf.Deg2Rad,b=(90-Mathf.Min(fill,(i+1)/96f)*360)*Mathf.Deg2Rad;
                var u=new Vector2(Mathf.Cos(a),Mathf.Sin(a));var v=new Vector2(Mathf.Cos(b),Mathf.Sin(b));int n=mesh.currentVertCount;
                mesh.AddVert(center+u*inner,color,Vector2.zero);mesh.AddVert(center+u*outer,color,Vector2.zero);
                mesh.AddVert(center+v*outer,color,Vector2.zero);mesh.AddVert(center+v*inner,color,Vector2.zero);
                mesh.AddTriangle(n,n+1,n+2);mesh.AddTriangle(n,n+2,n+3);
            }
        }
    }
}
