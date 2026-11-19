using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;
namespace CyberDive
{
    public static class SpaceArt
    {
        public static readonly Color Cyan=new Color(.25f,.92f,1f);
        static Material white,cyan,dither;
        static Transform stars;
        static Transform cameraRig;
        static Material White=>white?white:white=Make(Color.white);
        static Material Blue=>cyan?cyan:cyan=Make(new Color(Cyan.r*2.4f,Cyan.g*2.4f,Cyan.b*2.4f));
        static Material Hull=>dither?dither:dither=new Material(Shader.Find("CyberDive/DitherHull"));
        static Material Make(Color color)
        {
            var m=new Material(Resources.Load<Shader>("VectorUnlit"));m.SetColor("_BaseColor",color);return m;
        }
        public static Camera CreateCamera(Transform parent)
        {
            var obj=new GameObject("FlightCamera");obj.transform.SetParent(parent);
            var c=obj.AddComponent<Camera>();c.clearFlags=CameraClearFlags.SolidColor;c.backgroundColor=Color.black;
            c.fieldOfView=57;c.nearClipPlane=.1f;c.farClipPlane=180;obj.tag="MainCamera";
            obj.AddComponent<AudioListener>();
            obj.AddComponent<CinemachineBrain>();
            cameraRig=Root("CinemachineFlightRig",parent);
            var flight=cameraRig.gameObject.AddComponent<CinemachineCamera>();flight.Lens.FieldOfView=57;
            UpdateCamera(c,0,0);return c;
        }
        public static void UpdateCamera(Camera c,float impact,float time)
        {
            var target=cameraRig?cameraRig:c.transform;
            target.position=new Vector3(Mathf.Sin(time*53)*impact,5+Mathf.Cos(time*47)*impact,-11);
            target.LookAt(new Vector3(0,1,18));
        }
        static Transform Root(string name,Transform parent){var t=new GameObject(name).transform;t.SetParent(parent,false);return t;}
        static Mesh ShipMesh()
        {
            var m=new Mesh();m.vertices=new[]{new Vector3(0,0,2),new Vector3(-2,0,-1),new Vector3(-.5f,.3f,-.5f),new Vector3(0,.6f,-1),new Vector3(.5f,.3f,-.5f),new Vector3(2,0,-1),new Vector3(0,-.3f,-1)};
            m.triangles=new[]{0,2,1,0,3,2,0,4,3,0,5,4,0,6,5,0,1,6,1,2,6,2,3,6,3,4,6,4,5,6};m.RecalculateNormals();return m;
        }
        static Mesh CoreMesh()
        {
            var mesh=new Mesh{name="WardenOctahedron"};
            mesh.vertices=new[]{Vector3.up,Vector3.down,Vector3.left,Vector3.right,Vector3.forward,Vector3.back};
            mesh.triangles=new[]{0,4,3,0,2,4,0,5,2,0,3,5,1,3,4,1,4,2,1,2,5,1,5,3};mesh.RecalculateNormals();return mesh;
        }
        static Transform Faceted(Transform parent,Vector3 position,Vector3 scale)
        {
            var t=Root("FacetedArmor",parent);t.localPosition=position;t.localScale=scale;
            var mesh=CoreMesh();t.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;t.gameObject.AddComponent<MeshRenderer>().sharedMaterial=Hull;
            Points(t,mesh,500,.012f,White);Edges(t,mesh,.007f);return t;
        }
        public static Transform Ship(Transform parent)
        {
            var t=Root("Pilot",parent);var body=Root("DitherHull",t);Mesh m=ShipMesh();
            body.gameObject.AddComponent<MeshFilter>().sharedMesh=m;body.gameObject.AddComponent<MeshRenderer>().sharedMaterial=Hull;
            Points(body,m,1400,.018f,White);Edges(body,m,.014f);
            Faceted(t,new Vector3(0,.32f,.1f),new Vector3(.3f,.3f,.95f));
            for(int side=-1;side<=1;side+=2)
            {
                Faceted(t,new Vector3(side*1.15f,0,-.45f),new Vector3(.28f,.22f,.9f));
                Line(t,new[]{new Vector3(side*.45f,.02f,1.1f),new Vector3(side*1.75f,.02f,-.75f)},.025f,Blue);
            }
            for(int i=-1;i<=1;i+=2)Line(t,new[]{new Vector3(i*.45f,-.1f,-.6f),new Vector3(i*.45f,-.1f,-2.7f)},.07f,Blue);
            return t;
        }
        public static Transform Boss(Transform parent)
        {
            var t=Root("WardenCore",parent);t.position=new Vector3(0,1.2f,27);
            Faceted(t,Vector3.zero,Vector3.one*3);
            Faceted(t,Vector3.zero,Vector3.one*1.15f);
            for(int i=0;i<3;i++)Ring(t,4.5f+i*.7f,Quaternion.Euler(i*55,i*35,0),.025f,White);
            for(int i=0;i<6;i++)
            {
                float a=i*Mathf.PI/3;var position=new Vector3(Mathf.Cos(a)*5,Mathf.Sin(a)*5,0);
                Faceted(t,position,new Vector3(.55f,.55f,1.1f));
                Line(t,new[]{position*.65f,position*.9f},.035f,Blue);
            }
            return t;
        }
        public static Transform Drone(Transform parent)
        {
            var t=Root("Interceptor",parent);
            Faceted(t,Vector3.zero,new Vector3(1.1f,.65f,.7f));
            for(int i=-1;i<=1;i+=2)Line(t,new[]{new Vector3(i*.8f,0,.1f),new Vector3(i*1.3f,.35f,-.4f),new Vector3(i*1.3f,-.35f,-.4f),new Vector3(i*.8f,0,.1f)},.03f,White);
            return t;
        }
        public static Transform Shield(Transform parent)
        {
            var t=Root("ParryField",parent);
            for(int i=0;i<3;i++)Ring(t,2.1f,Quaternion.Euler(i*60,0,0),.024f,Blue);
            t.gameObject.SetActive(false);return t;
        }
        public static GameObject Projectile(Transform parent)=>Primitive(parent,PrimitiveType.Sphere,Vector3.zero,Vector3.one,White).gameObject;
        public static void MarkReflectable(GameObject obj,bool reflectable)
        {
            var trail=obj.GetComponent<TrailRenderer>();if(trail){trail.emitting=false;trail.Clear();}
            var marker=obj.transform.Find("ParryMarker");
            if(!marker){marker=Root("ParryMarker",obj.transform);Ring(marker,1.2f,Quaternion.identity,.08f,Blue);}
            marker.gameObject.SetActive(reflectable);
        }
        public static void ReflectionTrail(GameObject obj,bool enabled)
        {
            var trail=obj.GetComponent<TrailRenderer>();if(!trail)trail=obj.AddComponent<TrailRenderer>();
            trail.sharedMaterial=EnergyArt.Material;trail.startColor=Cyan;trail.endColor=new Color(Cyan.r,Cyan.g,Cyan.b,0);trail.time=.2f;trail.widthMultiplier=.13f;trail.emitting=enabled;
        }
        public static void ProjectileTrail(GameObject obj,bool friendly,bool reflectable,bool enabled)
        {
            var trail=obj.GetComponent<TrailRenderer>();if(!trail)trail=obj.AddComponent<TrailRenderer>();
            trail.sharedMaterial=EnergyArt.Material;trail.time=friendly?.12f:.2f;trail.widthMultiplier=friendly?.045f:reflectable?.10f:.045f;
            var color=reflectable?Cyan:Color.white;trail.startColor=new Color(color.r,color.g,color.b,.6f);trail.endColor=new Color(color.r,color.g,color.b,0);trail.emitting=enabled;trail.Clear();
        }
        public static void Tint(GameObject obj,Color color)=>obj.GetComponent<MeshRenderer>().sharedMaterial=color==Color.white?White:Blue;
        static Transform Primitive(Transform parent,PrimitiveType type,Vector3 pos,Vector3 scale,Material mat)
        {
            var obj=GameObject.CreatePrimitive(type);Object.Destroy(obj.GetComponent<Collider>());obj.transform.SetParent(parent,false);obj.transform.localPosition=pos;obj.transform.localScale=scale;
            obj.GetComponent<MeshRenderer>().sharedMaterial=mat;return obj.transform;
        }
        static void Line(Transform parent,Vector3[] vertices,float width,Material mat)
        {
            var l=Root("VectorTrace",parent).gameObject.AddComponent<LineRenderer>();l.useWorldSpace=false;l.sharedMaterial=mat;l.widthMultiplier=width;l.positionCount=vertices.Length;l.SetPositions(vertices);
        }
        static void Ring(Transform parent,float radius,Quaternion rotation,float width,Material mat)
        {
            var v=new Vector3[97];for(int i=0;i<v.Length;i++){float a=i*Mathf.PI*2/96;v[i]=rotation*new Vector3(Mathf.Cos(a)*radius,Mathf.Sin(a)*radius,0);}Line(parent,v,width,mat);
        }
        static void Edges(Transform parent,Mesh mesh,float width)
        {
            var v=mesh.vertices;var tris=mesh.triangles;var seen=new HashSet<string>();
            for(int i=0;i<tris.Length;i+=3)for(int j=0;j<3;j++)
            {int a=tris[i+j],b=tris[i+(j+1)%3];string key=Mathf.Min(a,b)+":"+Mathf.Max(a,b);if(seen.Add(key))Line(parent,new[]{v[a],v[b]},width,White);}
        }
        static void Points(Transform parent,Mesh source,int count,float size,Material mat)
        {
            var rng=new System.Random(731);var src=source.vertices;var ts=source.triangles;
            var vertices=new Vector3[count*4];var triangles=new int[count*6];
            for(int i=0;i<count;i++)
            {
                int t=rng.Next(ts.Length/3)*3;float a=(float)rng.NextDouble(),b=(float)rng.NextDouble();if(a+b>1){a=1-a;b=1-b;}
                Vector3 p=src[ts[t]]+a*(src[ts[t+1]]-src[ts[t]])+b*(src[ts[t+2]]-src[ts[t]]);
                Quad(vertices,triangles,i,p,size);
            }
            var mesh=new Mesh{name="SurfaceDots"};mesh.vertices=vertices;mesh.triangles=triangles;mesh.RecalculateBounds();
            var root=Root("PointCloud",parent);root.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;root.gameObject.AddComponent<MeshRenderer>().sharedMaterial=mat;
        }
        static void Quad(Vector3[] v,int[] tr,int i,Vector3 p,float size)
        {
            int n=i*4;v[n]=p+new Vector3(-size,-size,0);v[n+1]=p+new Vector3(size,-size,0);v[n+2]=p+new Vector3(size,size,0);v[n+3]=p+new Vector3(-size,size,0);
            int k=i*6;tr[k]=n;tr[k+1]=n+2;tr[k+2]=n+1;tr[k+3]=n;tr[k+4]=n+3;tr[k+5]=n+2;
        }
        public static void Environment(Transform parent,bool reduced)
        {
            var route=Root("NavigationVectors",parent);
            for(int side=-1;side<=1;side+=2)for(int z=8;z<100;z+=8)
            {Line(route,new[]{new Vector3(side*9,-2,z),new Vector3(side*9,-2,z+3)},.016f,Blue);}
            stars=Root("Starfield",parent);var rng=new System.Random(42);int count=1000;var v=new Vector3[count*4];var tr=new int[count*6];
            for(int i=0;i<count;i++)Quad(v,tr,i,new Vector3((float)rng.NextDouble()*100-50,(float)rng.NextDouble()*65-25,(float)rng.NextDouble()*130),i%8==0?.04f:.018f);
            var mesh=new Mesh{name="StarfieldDots"};mesh.vertices=v;mesh.triangles=tr;mesh.RecalculateBounds();stars.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;stars.gameObject.AddComponent<MeshRenderer>().sharedMaterial=White;
            for(int j=0;j<10;j++)
            {
                var rock=Primitive(parent,PrimitiveType.Sphere,new Vector3((j%2==0?-1:1)*(11+j%3*3),j%3-2,8+j*7),Vector3.one*(1+j%3),Hull);
                Points(rock,rock.GetComponent<MeshFilter>().sharedMesh,300,.012f,White);
            }
        }
        public static GameObject Spark(Transform parent)
        {return Primitive(parent,PrimitiveType.Cube,Vector3.zero,Vector3.one*.05f,White).gameObject;}
        public static void ScrollStars(float elapsed,bool reduced){if(stars){stars.localPosition=new Vector3(0,0,-elapsed*1.3f%25);stars.gameObject.SetActive(!reduced);}}
    }
}
