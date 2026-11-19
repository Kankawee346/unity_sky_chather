using UnityEngine;
using UnityEngine.UI;

// Small hand-authored pixel silhouettes: coin, crystal, potion, rune and heart.
public sealed class CatchLabPixelIcon : MaskableGraphic
{
    private int kind;
    public int Kind { get => kind; set { kind=Mathf.Clamp(value,0,4); SetVerticesDirty(); } }
    private static readonly string[][] Pixels = {
        new[]{"....OOOO....","..OOYYYYOO..",".OYYHYYYHYO.","OYHYHYYHYYYO","OYYHYYHYYYYO","OYYHYYHYYYYO","OYYHYYHYYYYO","OYYHYYHYYYYO","OYYYHHYYYYYO",".OYYYYYYYYO.","..OOYYYYOO..","....OOOO...."},
        new[]{".....OO.....","....OCCO....","...OCHHCO...","..OCCHHHCO..",".OCCCHHHHCO.","OCCCCHHHHHCO","OCCCCCHHHHCO",".OCCCCHHHCO.","..OCCCHHCO..","...OCCHCO...","....OCCO....",".....OO....."},
        new[]{"....OOOO....","....OYYO....","....OOOO....","....O..O....","...OHHHHO...","..OH....HO..",".OH.RRRR.HO.",".OH.RRRR.HO.",".OH.RRRR.HO.",".O.RRRRRR.O.","..ORRRRRRO..","...OOOOOO..."},
        new[]{"...OOOOOO...","..OYYYYYYO..",".OYYHYYHYYO.",".OYYHHHHYYO.",".OYYYHHYYYO.",".OYYYYHYYYO.",".OYYYHHYYYO.",".OYYHYYHYYO.",".OYYHYYHYYO.",".OYYYYYYYYO.","..OYYYYYYO..","...OOOOOO..."},
        new[]{"............","..OOO..OOO..",".ORRROORRRO.","ORHHRRRRRRRO","ORHRRRRRRRRO","ORRRRRRRRRRO",".ORRRRRRRRO.","..ORRRRRRO..","...ORRRRO...","....ORRO....",".....OO.....","............"}
    };
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear(); Rect r=rectTransform.rect; float cell=Mathf.Min(r.width,r.height)/12;
        var pattern=Pixels[kind];
        for(int y=0;y<12;y++) for(int x=0;x<12;x++)
        {
            char c=pattern[y][x]; if(c=='.') continue;
            Color tint=c switch { 'O'=>new Color32(31,18,38,255), 'Y'=>new Color32(242,178,66,255), 'C'=>new Color32(99,189,212,255), 'R'=>new Color32(224,77,94,255), _=>new Color32(255,239,190,255) };
            tint*=color;
            float px=r.center.x+(x-6)*cell, py=r.center.y+(5-y)*cell;
            int n=vh.currentVertCount;
            vh.AddVert(new Vector3(px,py),tint,Vector2.zero); vh.AddVert(new Vector3(px,py+cell),tint,Vector2.zero);
            vh.AddVert(new Vector3(px+cell,py+cell),tint,Vector2.zero); vh.AddVert(new Vector3(px+cell,py),tint,Vector2.zero);
            vh.AddTriangle(n,n+1,n+2); vh.AddTriangle(n,n+2,n+3);
        }
    }
}
