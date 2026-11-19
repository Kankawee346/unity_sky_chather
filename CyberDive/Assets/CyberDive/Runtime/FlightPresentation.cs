using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace CyberDive
{
    public sealed class FlightPresentation : MonoBehaviour
    {
        VolumeProfile profile;Bloom bloom;Volume volume;ParticleSystem[] engines;
        public void Initialize(Camera camera,Transform pilot)
        {
            camera.allowHDR=true;var data=camera.GetUniversalAdditionalCameraData();data.renderPostProcessing=true;data.volumeLayerMask=1;
            var obj=new GameObject("FlightPostFX");obj.transform.SetParent(transform,false);volume=obj.AddComponent<Volume>();volume.isGlobal=true;volume.priority=20;
            profile=ScriptableObject.CreateInstance<VolumeProfile>();volume.sharedProfile=profile;
            bloom=profile.Add<Bloom>(true);bloom.threshold.value=1.1f;bloom.intensity.value=.65f;bloom.scatter.value=.55f;
            var vignette=profile.Add<Vignette>(true);vignette.intensity.value=.21f;vignette.smoothness.value=.5f;
            engines=new ParticleSystem[2];
            for(int i=0;i<2;i++)
            {
                var nozzle=new GameObject("IonExhaust");nozzle.transform.SetParent(pilot,false);nozzle.transform.localPosition=new Vector3(i==0?-.45f:.45f,-.1f,-.85f);nozzle.transform.localRotation=Quaternion.LookRotation(Vector3.back);
                var particles=nozzle.AddComponent<ParticleSystem>();particles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
                var main=particles.main;main.loop=true;main.duration=1;main.startLifetime=.3f;main.startSpeed=7;main.startSize=.14f;main.startColor=SpaceArt.Cyan;main.maxParticles=40;main.simulationSpace=ParticleSystemSimulationSpace.Local;
                var emission=particles.emission;emission.rateOverTime=45;
                var shape=particles.shape;shape.shapeType=ParticleSystemShapeType.Cone;shape.angle=5;shape.radius=.07f;
                var size=particles.sizeOverLifetime;size.enabled=true;size.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.Linear(0,1,1,0));
                var color=particles.colorOverLifetime;color.enabled=true;var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(SpaceArt.Cyan,0),new GradientColorKey(SpaceArt.Cyan,1)},new[]{new GradientAlphaKey(.8f,0),new GradientAlphaKey(0,1)});color.color=gradient;
                var renderer=particles.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=EnergyArt.Material;renderer.renderMode=ParticleSystemRenderMode.Stretch;renderer.lengthScale=2;renderer.velocityScale=.05f;
                engines[i]=particles;
            }
        }
        public void SetState(bool reduced,bool playing)
        {
            if(bloom)bloom.active=!reduced;
            if(engines==null)return;
            foreach(var engine in engines)
            {
                if(playing && !reduced){if(!engine.isPlaying)engine.Play();}
                else if(engine.isPlaying)engine.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }
        void OnDestroy(){if(profile){foreach(var component in profile.components)Destroy(component);Destroy(profile);}}
    }
}
