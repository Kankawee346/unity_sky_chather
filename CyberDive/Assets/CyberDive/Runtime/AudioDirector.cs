using UnityEngine;
namespace CyberDive
{
    public sealed class AudioDirector : MonoBehaviour
    {
        AudioSource source;AudioClip beat,parry,reflect,hit,kill,win,lose;
        void Awake(){source=gameObject.AddComponent<AudioSource>();source.volume=.12f;beat=Tone(85,.09f);parry=Tone(520,.15f);reflect=Tone(1040,.18f);hit=Tone(48,.22f);kill=Tone(210,.16f);win=Tone(880,.6f);lose=Tone(36,.6f);}
        static AudioClip Tone(float hz,float length)
        {
            int count=(int)(44100*length);var values=new float[count];
            for(int i=0;i<count;i++){float t=i/44100f;values[i]=Mathf.Sin(t*hz*2*Mathf.PI)*Mathf.Exp(-t*25)*Mathf.Min(1,t*1000);}
            var c=AudioClip.Create("OriginalSynthPulse",count,1,44100,false);c.SetData(values,0);return c;
        }
        public void Beat(int index,bool reduced){source.PlayOneShot(beat,index%4==0?1:.45f);}
        public void SetMuted(bool muted)=>source.mute=muted;
        public void Parry()=>source.PlayOneShot(parry);
        public void Reflect()=>source.PlayOneShot(reflect);
        public void Hit()=>source.PlayOneShot(hit);
        public void Kill()=>source.PlayOneShot(kill,.7f);
        public void Finish(bool won)=>source.PlayOneShot(won?win:lose);
    }
}
