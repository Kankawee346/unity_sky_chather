using UnityEngine;
namespace CyberDive
{
    public sealed class HudScanlines : UnityEngine.UI.MaskableGraphic
    {
        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper mesh)
        {
            mesh.Clear();var r=rectTransform.rect;
            for(float y=r.yMin;y<r.yMax;y+=6)
            {
                int n=mesh.currentVertCount;mesh.AddVert(new Vector3(r.xMin,y),color,Vector2.zero);mesh.AddVert(new Vector3(r.xMax,y),color,Vector2.zero);
                mesh.AddVert(new Vector3(r.xMax,y+1),color,Vector2.zero);mesh.AddVert(new Vector3(r.xMin,y+1),color,Vector2.zero);mesh.AddTriangle(n,n+1,n+2);mesh.AddTriangle(n,n+2,n+3);
            }
        }
    }
}
