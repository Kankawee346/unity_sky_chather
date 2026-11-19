using System.Collections.Generic;
using UnityEngine;
namespace CyberDive
{
    // Original 5x7 block lettering; layered outline offsets create the depth in the supplied reference.
    public sealed class BlockTitle : UnityEngine.UI.MaskableGraphic
    {
        string caption="CYBER\nDIVE";
        public string Caption{get=>caption;set{if(caption==value)return;caption=value;SetVerticesDirty();}}
        static readonly Dictionary<char,string> Glyphs=new Dictionary<char,string>
        {
            {'A',"01110100011000111111100011000110001"},{'B',"11110100011000111110100011000111110"},
            {'C',"01111100001000010000100001000001111"},{'D',"11110100011000110001100011000111110"},
            {'E',"11111100001000011110100001000011111"},{'F',"11111100001000011110100001000010000"},
            {'G',"01111100001000010111100011000101111"},{'H',"10001100011000111111100011000110001"},
            {'I',"11111001000010000100001000010011111"},{'L',"10000100001000010000100001000011111"},
            {'M',"10001110111010110101100011000110001"},{'N',"10001110011010110011100011000110001"},
            {'O',"01110100011000110001100011000101110"},{'P',"11110100011000111110100001000010000"},
            {'R',"11110100011000111110101001001010001"},{'S',"01111100001000001110000010000111110"},
            {'T',"11111001000010000100001000010000100"},{'U',"10001100011000110001100011000101110"},
            {'V',"10001100011000110001100010101000100"},{'Y',"10001100010101000100001000010000100"}
        };
        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper mesh)
        {
            mesh.Clear();var lines=caption.ToUpperInvariant().Split('\n');int longest=1;foreach(var line in lines)longest=Mathf.Max(longest,line.Length);
            float unit=Mathf.Min((rectTransform.rect.width-22)/(longest*6-1),(rectTransform.rect.height-22)/(lines.Length*9-2));
            Layer(mesh,lines,unit,new Vector2(10,-10),new Color(.6f,.8f,.85f,.38f),2);
            Layer(mesh,lines,unit,new Vector2(10,-10),new Color(.005f,.014f,.02f,1),0);
            Layer(mesh,lines,unit,new Vector2(5,-5),new Color(.75f,.9f,.94f,.85f),1.2f);
            Layer(mesh,lines,unit,new Vector2(5,-5),new Color(.005f,.014f,.02f,1),0);
            Layer(mesh,lines,unit,Vector2.zero,color,0);
        }
        void Layer(UnityEngine.UI.VertexHelper mesh,string[] lines,float unit,Vector2 offset,Color shade,float expand)
        {
            var rect=rectTransform.rect;
            for(int row=0;row<lines.Length;row++)for(int c=0;c<lines[row].Length;c++)
            {
                if(!Glyphs.TryGetValue(lines[row][c],out string bits))continue;
                for(int p=0;p<bits.Length;p++)if(bits[p]=='1')
                {
                    float x=rect.xMin+(c*6+p%5)*unit+offset.x,y=rect.yMax-(row*9+p/5)*unit+offset.y;
                    Quad(mesh,x-expand,y+expand,unit+expand*2+.15f,unit+expand*2+.15f,shade);
                }
            }
        }
        static void Quad(UnityEngine.UI.VertexHelper mesh,float x,float y,float w,float h,Color color)
        {
            int n=mesh.currentVertCount;mesh.AddVert(new Vector3(x,y),color,Vector2.zero);mesh.AddVert(new Vector3(x+w,y),color,Vector2.zero);
            mesh.AddVert(new Vector3(x+w,y-h),color,Vector2.zero);mesh.AddVert(new Vector3(x,y-h),color,Vector2.zero);mesh.AddTriangle(n,n+1,n+2);mesh.AddTriangle(n,n+2,n+3);
        }
    }
}
