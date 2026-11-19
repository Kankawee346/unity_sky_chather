using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
namespace CyberDive
{
    public sealed class DiveGame : MonoBehaviour
    {
        public RunState State {get;}=new RunState();
        public float Elapsed {get;private set;}
        public int ActiveShots => shots.FindAll(s=>s.live).Count;
        public int BossHealth {get;private set;}=MissionRules.BossMaxHealth;
        public int BestScore {get;private set;}
        public CampaignDifficulty SelectedDifficulty {get;private set;}=CampaignDifficulty.Normal;
        public CampaignDifficulty RunDifficulty {get;private set;}=CampaignDifficulty.Normal;
        public bool Muted {get;private set;}
        public bool ReducedEffects,Shake=true;
        public bool FlashEffects;
        public float ScreenPulse {get;private set;}
        public string Feedback {get;private set;}="AWAITING PILOT";
        public BridgeClient Bridge {get;private set;}
        public float PilotX=>ship?ship.position.x:0;
        public float FrameMs {get;private set;}
        public bool BossVisible=>MissionRules.SectorAt(Elapsed)==Sector.Warden;
        public string SectorName=>MissionRules.SectorAt(Elapsed)==Sector.Approach?"01 / APPROACH":BossVisible?(MissionRules.BossPhase(BossHealth)==1?"03 / WARDEN":"03 / WARDEN ENRAGED"):"02 / FIREWALL";
        public float WarningX {get;private set;}
        public bool Warning {get;private set;}
        public float ParryArrival {get;private set;}=99;
        Transform ship,boss,shield,world;
        Camera view;
        TerminalHud hud;
        AudioDirector audioDirector;
        CombatFeedback combatFeedback;
        FlightPresentation presentation;
        double origin,pausedAt,nextFire,nextBeat;
        int beat;
        float feedbackUntil,impact;
        bool recorded;
        Sector previousSector;
        class Shot { public GameObject obj;public Vector3 velocity;public bool live,friendly,reflectable;public float radius; }
        class Drone { public Transform obj;public bool live;public int hp; }
        readonly List<Shot> shots=new List<Shot>();
        readonly List<Drone> drones=new List<Drone>();
        double Now=>AudioSettings.dspTime;
        void Awake()
        {
            Application.runInBackground=true;Application.targetFrameRate=60;
            BestScore=PlayerPrefs.GetInt("cyber-dive-best",0);
            SelectedDifficulty=DifficultyRules.FromSaved(PlayerPrefs.GetInt("cyber-option-difficulty",1));
            RunDifficulty=SelectedDifficulty;
            Muted=PlayerPrefs.GetInt("cyber-option-muted",0)!=0;
            ReducedEffects=PlayerPrefs.GetInt("cyber-option-lowfx",0)!=0;
            Shake=PlayerPrefs.GetInt("cyber-option-shake",1)!=0;
            FlashEffects=PlayerPrefs.GetInt("cyber-option-flash",0)!=0;
            world=new GameObject("GeneratedWorld").transform;world.SetParent(transform);
            view=SpaceArt.CreateCamera(world);
            ship=SpaceArt.Ship(world);boss=SpaceArt.Boss(world);shield=SpaceArt.Shield(ship);
            presentation=gameObject.AddComponent<FlightPresentation>();presentation.Initialize(view,ship);presentation.SetState(ReducedEffects,false);
            SpaceArt.Environment(world,ReducedEffects);
            Bridge=gameObject.AddComponent<BridgeClient>();
            hud=gameObject.AddComponent<TerminalHud>();hud.Game=this;
            audioDirector=gameObject.AddComponent<AudioDirector>();
            audioDirector.SetMuted(Muted);
            combatFeedback=gameObject.AddComponent<CombatFeedback>();
            boss.gameObject.SetActive(false);
        }
        public void BeginRun()
        {
            RecordResult();
            RunDifficulty=SelectedDifficulty;
            combatFeedback.Clear();ScreenPulse=0;
            foreach(var s in shots){s.live=false;s.obj.SetActive(false);}
            foreach(var d in drones){d.live=false;d.obj.gameObject.SetActive(false);}
            origin=Now;Elapsed=0;nextFire=0;nextBeat=1;beat=0;BossHealth=MissionRules.BossMaxHealth;recorded=false;previousSector=Sector.Approach;
            ship.position=Vector3.zero;State.Start(Now);Bridge.Disarm();
            Feedback="BREACH INITIATED";feedbackUntil=3;Warning=false;
        }
        public bool TrySelectDifficulty(int value)
        {
            if(value<0 || value>2 || State.Phase==RunPhase.Playing || State.Phase==RunPhase.Paused)return false;
            SelectedDifficulty=(CampaignDifficulty)value;SaveOptions();return true;
        }
        public void ToggleMute(){Muted=!Muted;audioDirector.SetMuted(Muted);SaveOptions();}
        public void SaveOptions()
        {
            PlayerPrefs.SetInt("cyber-option-difficulty",(int)SelectedDifficulty);
            PlayerPrefs.SetInt("cyber-option-muted",Muted?1:0);
            PlayerPrefs.SetInt("cyber-option-lowfx",ReducedEffects?1:0);
            PlayerPrefs.SetInt("cyber-option-shake",Shake?1:0);
            PlayerPrefs.SetInt("cyber-option-flash",FlashEffects?1:0);
            PlayerPrefs.Save();
        }
        public void TogglePause()
        {
            bool pause=State.Phase==RunPhase.Playing;
            if(pause)pausedAt=Now;
            else if(State.Phase==RunPhase.Paused)origin+=Now-pausedAt;
            State.Pause(pause,Now);Bridge.Disarm();
        }
        void Update()
        {
            FrameMs=Mathf.Lerp(FrameMs,Time.unscaledDeltaTime*1000,.03f);
            var keys=Keyboard.current;
            if(keys!=null)
            {
                if(keys.enterKey.wasPressedThisFrame && State.Phase!=RunPhase.Playing && State.Phase!=RunPhase.Paused)BeginRun();
                if(keys.escapeKey.wasPressedThisFrame && (State.Phase==RunPhase.Playing || State.Phase==RunPhase.Paused))TogglePause();
                if(keys.f1Key.wasPressedThisFrame)ReducedEffects=!ReducedEffects;
                if(keys.f2Key.wasPressedThisFrame)Shake=!Shake;
                if(keys.f3Key.wasPressedThisFrame)FlashEffects=!FlashEffects;
                if(keys.f1Key.wasPressedThisFrame || keys.f2Key.wasPressedThisFrame || keys.f3Key.wasPressedThisFrame)SaveOptions();
                if(keys.mKey.wasPressedThisFrame)ToggleMute();
                if(keys.f12Key.wasPressedThisFrame)ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(Application.dataPath,"../preview.png"));
            }
            Bridge.AcceptSkills=State.Phase==RunPhase.Playing;
            presentation.SetState(ReducedEffects,State.Phase==RunPhase.Playing);
            if(State.Phase!=RunPhase.Playing){Bridge.Disarm();shield.gameObject.SetActive(false);combatFeedback.Clear();ScreenPulse=0;RecordResult();return;}
            if(ReducedEffects)combatFeedback.Clear();
            State.Tick(Now);Elapsed=(float)(Now-origin);
            if(MissionRules.Expired(Elapsed))
            {
                State.Fail();Feedback="MISSION TIMEOUT";Bridge.Disarm();shield.gameObject.SetActive(false);RecordResult();return;
            }
            var sector=MissionRules.SectorAt(Elapsed);
            if(sector!=previousSector){previousSector=sector;Feedback=BossVisible?"WARNING // WARDEN CORE":"SECTOR 02 // FIREWALL";feedbackUntil=Elapsed+3;}
            float input=Bridge.Direction;
            if(keys!=null && (keys.aKey.isPressed || keys.dKey.isPressed || keys.leftArrowKey.isPressed || keys.rightArrowKey.isPressed))
            {bool left=keys.aKey.isPressed||keys.leftArrowKey.isPressed,right=keys.dKey.isPressed||keys.rightArrowKey.isPressed;input=left==right?0:left?-1:1;}
            ship.position=new Vector3(Mathf.Clamp(ship.position.x+input*8*Time.deltaTime,-7,7),0,0);
            ship.rotation=Quaternion.Slerp(ship.rotation,Quaternion.Euler(0,0,-input*22),Time.deltaTime*10);
            bool pose=Bridge.ConsumeParry();
            if(pose || (keys!=null && keys.spaceKey.wasPressedThisFrame))
            {
                if(State.TryParry(Now)){Feedback="SHIELD // ACTIVE";feedbackUntil=Elapsed+.6f;audioDirector.Parry();if(!ReducedEffects)combatFeedback.Emit(ship.position+Vector3.forward,true,true);}
            }
            shield.gameObject.SetActive(State.ShieldActive);
            shield.Rotate(0,0,50*Time.deltaTime);
            boss.gameObject.SetActive(BossVisible);
            if(BossVisible){boss.position=new Vector3(Mathf.Sin(Elapsed*.55f)*3,1.2f,27);boss.Rotate(8*Time.deltaTime,12*Time.deltaTime,9*Time.deltaTime);}
            if(Elapsed>=nextFire){nextFire=Elapsed+(State.OverdriveActive?.09:.18);SpawnShot(ship.position+Vector3.forward*1.8f,Vector3.forward*38,true,false);}
            if(Elapsed>=nextBeat){OnBeat();nextBeat=Elapsed+.5;}
            Warning=false;ParryArrival=99;
            foreach(var shot in shots)
            {
                if(!shot.live || shot.friendly || !shot.reflectable || shot.obj.transform.position.z<0)continue;
                float arrival=shot.obj.transform.position.z/Mathf.Abs(shot.velocity.z);
                if(arrival<ParryArrival){ParryArrival=arrival;WarningX=shot.obj.transform.position.x;}
            }
            Warning=ParryArrival<1.0f;
            UpdateShots();UpdateDrones();
            combatFeedback.Tick(Time.deltaTime);ScreenPulse=Mathf.MoveTowards(ScreenPulse,0,Time.deltaTime*4);
            impact=Mathf.MoveTowards(impact,0,Time.deltaTime*2);
            SpaceArt.UpdateCamera(view,Shake?impact:0,Elapsed);
            SpaceArt.ScrollStars(Elapsed,ReducedEffects);
            if(Elapsed>feedbackUntil)Feedback=State.OverdriveActive?"OVERDRIVE // 200%":"";
        }
        void RecordResult()
        {
            if(recorded || (State.Phase!=RunPhase.Won && State.Phase!=RunPhase.Lost))return;
            recorded=true;
            audioDirector.Finish(State.Phase==RunPhase.Won);
            if(State.Score>BestScore){BestScore=State.Score;PlayerPrefs.SetInt("cyber-dive-best",BestScore);PlayerPrefs.Save();}
        }
        void OnBeat()
        {
            beat++;audioDirector.Beat(beat,ReducedEffects);
            int regular=DifficultyRules.VolleyBeats(RunDifficulty,false);
            if(!BossVisible)
            {
                if(beat%4==0)SpawnDrone(Elapsed<12?(beat%8==0?-5:5):Mathf.Sin(beat*2.1f)*6);
                if(beat%regular==0)SpawnShot(new Vector3(ship.position.x,0,28),Vector3.back*12,false,true);
                if(MissionRules.SectorAt(Elapsed)==Sector.Firewall && beat%regular==regular/2)
                {
                    int gap=(beat/regular)%3-1;
                    for(int lane=-2;lane<=2;lane++)if(lane!=gap)SpawnShot(new Vector3(lane*2.7f,0,30),Vector3.back*11,false,false);
                }
            }
            else
            {
                int phase=MissionRules.BossPhase(BossHealth);
                if(beat%DifficultyRules.VolleyBeats(RunDifficulty,phase!=1)==0)
                {
                    WarningX=ship.position.x;
                    for(int i=-3;i<=3;i++)SpawnShot(new Vector3(WarningX+i*2.4f,0,24),Vector3.back*(14+Mathf.Abs(i)),false,i==0);
                }
                if(beat%8==2)SpawnDrone(Mathf.Sin(beat)*5);
            }
        }
        void SpawnDrone(float x)
        {
            var d=drones.Find(v=>!v.live);
            if(d==null){d=new Drone{obj=SpaceArt.Drone(world)};drones.Add(d);}
            d.live=true;d.hp=2;d.obj.gameObject.SetActive(true);d.obj.position=new Vector3(x,0,30);
        }
        void SpawnShot(Vector3 pos,Vector3 velocity,bool friendly,bool reflectable)
        {
            var s=shots.Find(v=>!v.live);
            if(s==null){s=new Shot{obj=SpaceArt.Projectile(world)};shots.Add(s);}
            s.live=true;s.velocity=velocity*(friendly?1:DifficultyRules.SpeedFactor(RunDifficulty));s.friendly=friendly;s.reflectable=reflectable;s.radius=reflectable?.55f:.32f;
            s.obj.SetActive(true);s.obj.transform.position=pos;
            s.obj.transform.localScale=friendly?new Vector3(.09f,.09f,.8f):Vector3.one*(reflectable?.65f:.4f);
            SpaceArt.Tint(s.obj,reflectable?SpaceArt.Cyan:Color.white);
            SpaceArt.MarkReflectable(s.obj,reflectable);
            SpaceArt.ProjectileTrail(s.obj,friendly,reflectable,!ReducedEffects);
        }
        void Retire(Shot s){s.live=false;s.obj.SetActive(false);}
        void UpdateShots()
        {
            foreach(var s in shots)
            {
                if(!s.live)continue;
                var trail=s.obj.GetComponent<TrailRenderer>();if(trail){trail.emitting=!ReducedEffects;if(ReducedEffects)trail.Clear();}
                Vector3 before=s.obj.transform.position;
                s.obj.transform.position+=s.velocity*Time.deltaTime;
                var p=s.obj.transform.position;
                if(s.friendly)
                {
                    foreach(var d in drones)
                    {
                        if(d.live && Mathf.Abs(p.x-d.obj.position.x)<1.1f && before.z<=d.obj.position.z+1 && p.z>=d.obj.position.z-1)
                        {Retire(s);if(!ReducedEffects)combatFeedback.Emit(d.obj.position,false,false);if(--d.hp<=0){if(!ReducedEffects)combatFeedback.Emit(d.obj.position,true,true);audioDirector.Kill();d.live=false;d.obj.gameObject.SetActive(false);State.AwardKill(50);}break;}
                    }
                    if(s.live && BossVisible && before.z<=boss.position.z+2 && p.z>=boss.position.z-2 && Mathf.Abs(p.x-boss.position.x)<3.2f)
                    {Retire(s);if(!ReducedEffects)combatFeedback.Emit(p,false,false);BossHealth-=State.OverdriveActive?3:1;if(BossHealth<=0){BossHealth=0;State.DefeatBoss();Feedback="BREACH COMPLETE";}}
                }
                else if(before.z>=0 && p.z<=0)
                {
                    if(Mathf.Abs(p.x-ship.position.x)<(State.ShieldActive && s.reflectable?2.1f:.75f))
                    {
                        if(s.reflectable && State.Reflect(Now))
                        {
                            s.friendly=true;s.reflectable=false;s.velocity=(boss.position-p).normalized*48;
                            SpaceArt.Tint(s.obj,SpaceArt.Cyan);Feedback="PERFECT PARRY";feedbackUntil=Elapsed+1;
                            SpaceArt.ReflectionTrail(s.obj,!ReducedEffects);
                            ScreenPulse=.18f;if(!ReducedEffects)combatFeedback.Emit(p,true,true);
                            audioDirector.Reflect();
                        }
                        else {Hit();Retire(s);}
                    }
                }
                if(p.z< -10 || p.z>55)Retire(s);
            }
        }
        void UpdateDrones()
        {
            foreach(var d in drones)
            {
                if(!d.live)continue;
                float old=d.obj.position.z;
                d.obj.position+=Vector3.back*9*DifficultyRules.SpeedFactor(RunDifficulty)*Time.deltaTime;d.obj.Rotate(0,60*Time.deltaTime,20*Time.deltaTime);
                if(old>=0 && d.obj.position.z<=0){if(Mathf.Abs(d.obj.position.x-ship.position.x)<1.2f)Hit();}
                if(d.obj.position.z< -6){d.live=false;d.obj.gameObject.SetActive(false);}
            }
        }
        void Hit(){if(State.Damage(Now)){impact=.6f;ScreenPulse=.35f;if(!ReducedEffects)combatFeedback.Emit(ship.position,false,true);Feedback="HULL DAMAGE";feedbackUntil=Elapsed+1;audioDirector.Hit();}}
    }
}
