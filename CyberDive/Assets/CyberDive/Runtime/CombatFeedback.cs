using UnityEngine;
namespace CyberDive
{
    public sealed class CombatFeedback : MonoBehaviour
    {
        const int Capacity=12,Shards=14;
        sealed class Burst { public Transform root;public LineRenderer ring,echo;public LineRenderer[] streaks;public Vector3[] directions;public Color color;public float age,duration,size;public bool active; }
        readonly Burst[] bursts=new Burst[Capacity];int cursor;
        public int ActiveCount {get{int count=0;foreach(var b in bursts)if(b!=null&&b.active)count++;return count;}}
        void Awake()
        {
            var random=new System.Random(542);
            for(int n=0;n<Capacity;n++)
            {
                var root=new GameObject("ImpactPool").transform;root.SetParent(transform,false);
                var b=new Burst{root=root,streaks=new LineRenderer[Shards],directions=new Vector3[Shards]};bursts[n]=b;
                b.ring=EnergyArt.Trace(root,65,.06f);b.echo=EnergyArt.Trace(root,65,.025f);
                for(int i=0;i<Shards;i++)
                {
                    float a=i*Mathf.PI*2/Shards+(float)random.NextDouble()*.3f;
                    b.directions[i]=new Vector3(Mathf.Cos(a),Mathf.Sin(a),(float)random.NextDouble()*.4f-.2f)*(2.5f+(float)random.NextDouble()*4);
                    b.streaks[i]=EnergyArt.Trace(root,2,.05f);
                }
                root.gameObject.SetActive(false);
            }
        }
        public void Emit(Vector3 position,bool cyan,bool large)
        {
            var b=bursts[cursor];cursor=(cursor+1)%Capacity;b.age=0;b.active=true;b.duration=large?.65f:.26f;b.size=large?1.5f:.45f;b.color=cyan?SpaceArt.Cyan:Color.white;
            b.root.position=position;b.root.rotation=Camera.main?Camera.main.transform.rotation:Quaternion.identity;b.root.gameObject.SetActive(true);Draw(b);
        }
        static void Draw(Burst b)
        {
            float t=Mathf.Clamp01(b.age/b.duration),fade=(1-t)*(1-t),radius=(.2f+Mathf.Sqrt(t)*3)*b.size;
            var color=new Color(b.color.r,b.color.g,b.color.b,fade);
            EnergyArt.Circle(b.ring,radius);EnergyArt.Circle(b.echo,radius*.73f);
            b.ring.startColor=b.ring.endColor=color;b.echo.startColor=b.echo.endColor=new Color(color.r,color.g,color.b,fade*.4f);
            b.ring.widthMultiplier=.065f*(1-t);b.echo.widthMultiplier=.025f*(1-t);
            for(int i=0;i<Shards;i++)
            {
                var line=b.streaks[i];var direction=b.directions[i]*b.size;
                line.SetPosition(0,direction*Mathf.Max(0,b.age-.07f));line.SetPosition(1,direction*(b.age+.025f));
                line.startColor=color;line.endColor=new Color(color.r,color.g,color.b,0);line.widthMultiplier=.055f*(1-t);
            }
        }
        public void Tick(float delta)
        {
            foreach(var b in bursts)
            {
                if(b==null || !b.active)continue;b.age+=Mathf.Max(0,delta);
                if(b.age>=b.duration){b.active=false;b.root.gameObject.SetActive(false);}else Draw(b);
            }
        }
        public void Clear(){foreach(var b in bursts)if(b!=null){b.active=false;b.root.gameObject.SetActive(false);}}
    }
}
