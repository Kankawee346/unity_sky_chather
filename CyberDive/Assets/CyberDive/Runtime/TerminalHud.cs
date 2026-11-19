using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
namespace CyberDive
{
    public sealed class TerminalHud : MonoBehaviour
    {
        public DiveGame Game;
        static readonly Color Ink=new Color(.018f,.035f,.05f,.91f),Muted=new Color(.48f,.61f,.66f),Accent=new Color(.22f,.92f,1);
        Font font;RectTransform canvas,flight,panel;
        Text score,best,timer,sector,objective,bossCaption,feedback,telemetry,parryValue,syncValue,syncCaption;
        Text menuSub,summary,startText,difficultyText,audioText,modeText,fxText,shakeText,flashText;BlockTitle title;
        Image flash,bossFill,syncFill;readonly Image[] hull=new Image[3];
        HudRing charge,heroRing;CanvasGroup menuFade;UnityEngine.UI.Button difficultyButton;
        int inputMode;bool showTelemetry;
        void Start()
        {
            font=Resources.Load<Font>("CyberMono")??Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var root=new GameObject("TerminalHUD",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));root.transform.SetParent(transform,false);
            root.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
            var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
            canvas=root.GetComponent<RectTransform>();flash=Box(canvas,"OptionalFlash",Vector2.zero,new Vector2(1920,1080),Color.clear);
            var es=FindAnyObjectByType<EventSystem>();
            if(!es){var o=new GameObject("EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));o.transform.SetParent(transform);es=o.GetComponent<EventSystem>();}
            es.sendNavigationEvents=false; // DiveGame owns keyboard commands; menus use pointer input.
            BuildFlight();BuildMenu();
        }
        void BuildFlight()
        {
            flight=Rect(canvas,"FlightHUD",Vector2.zero,new Vector2(1920,1080));
            var scoring=Card(flight,"ScoreCard",new Vector2(48,40),new Vector2(390,156));
            TextAt(scoring,"Caption","CYBER // DIVE",new Vector2(24,15),new Vector2(340,24),17,Accent);
            score=TextAt(scoring,"Score","000000",new Vector2(24,40),new Vector2(340,65),49,Color.white);
            best=TextAt(scoring,"Best","",new Vector2(24,115),new Vector2(345,24),16,Muted);
            sector=TextAt(flight,"Sector","",new Vector2(610,48),new Vector2(700,32),22,Accent,TextAnchor.MiddleCenter);
            var track=Box(flight,"BossTrack",new Vector2(610,94),new Vector2(700,12),new Color(.12f,.2f,.23f,.8f));
            bossFill=Box(track.rectTransform,"Fill",Vector2.zero,new Vector2(700,12),Accent);
            for(int i=1;i<10;i++)Box(flight,"TrackDivision",new Vector2(610+i*70,94),new Vector2(3,12),Color.black);
            bossCaption=TextAt(flight,"BossCaption","",new Vector2(610,119),new Vector2(700,24),15,Muted,TextAnchor.MiddleCenter);
            var clock=Card(flight,"Clock",new Vector2(1518,40),new Vector2(354,156));
            TextAt(clock,"Caption","TIME REMAINING",new Vector2(24,15),new Vector2(306,24),16,Muted,TextAnchor.MiddleRight);
            timer=TextAt(clock,"Time","02:00",new Vector2(24,43),new Vector2(306,78),60,Color.white,TextAnchor.MiddleRight);
            objective=TextAt(flight,"Objective","",new Vector2(610,160),new Vector2(700,32),18,Color.white,TextAnchor.MiddleCenter);
            var survival=Card(flight,"Survival",new Vector2(48,872),new Vector2(390,142));
            TextAt(survival,"Caption","HULL INTEGRITY",new Vector2(24,18),new Vector2(340,28),18,Muted);
            for(int i=0;i<3;i++)hull[i]=Box(survival,"Hull"+(i+1),new Vector2(24+i*114,65),new Vector2(100,18),Color.white);
            TextAt(survival,"Help","A / D   STEER",new Vector2(24,105),new Vector2(340,24),16,Color.white);
            var parry=Rect(flight,"Parry",new Vector2(850,844),new Vector2(220,185));
            Ring(parry,"Track",new Vector2(43,0),new Vector2(134,134),new Color(.13f,.23f,.26f),7);
            charge=Ring(parry,"Charge",new Vector2(43,0),new Vector2(134,134),Accent,7);
            Ring(parry,"Inner",new Vector2(54,11),new Vector2(112,112),new Color(.2f,.6f,.65f,.4f),1);
            parryValue=TextAt(parry,"Value","READY",new Vector2(0,41),new Vector2(220,50),27,Accent,TextAnchor.MiddleCenter);
            TextAt(parry,"Help","SPACE / PARRY",new Vector2(0,151),new Vector2(220,26),18,Color.white,TextAnchor.MiddleCenter);
            var sync=Card(flight,"Sync",new Vector2(1482,872),new Vector2(390,142));
            TextAt(sync,"Caption","SYNC DRIVE",new Vector2(24,18),new Vector2(210,28),18,Muted);
            syncValue=TextAt(sync,"Multiplier","x01",new Vector2(230,9),new Vector2(132,60),43,Accent,TextAnchor.MiddleRight);
            Box(sync,"Track",new Vector2(24,76),new Vector2(342,10),new Color(.12f,.2f,.23f));
            syncFill=Box(sync,"Fill",new Vector2(24,76),new Vector2(342,10),Accent);
            syncCaption=TextAt(sync,"Status","",new Vector2(24,105),new Vector2(342,24),15,Color.white);
            feedback=TextAt(flight,"CombatCue","",new Vector2(510,746),new Vector2(900,65),32,Accent,TextAnchor.MiddleCenter);
            telemetry=TextAt(flight,"Telemetry","",new Vector2(50,230),new Vector2(450,240),18,Muted);telemetry.gameObject.SetActive(false);
            TextAt(flight,"SettingsHelp","ESC PAUSE    M AUDIO    F4 TELEMETRY",new Vector2(1090,1035),new Vector2(780,25),15,Muted,TextAnchor.MiddleRight);
        }
        void BuildMenu()
        {
            panel=Rect(canvas,"MissionPanel",Vector2.zero,new Vector2(1920,1080));
            Box(panel,"Backdrop",Vector2.zero,new Vector2(1920,1080),new Color(.005f,.014f,.025f,.87f));
            var scan=Rect(panel,"Scanlines",Vector2.zero,new Vector2(1920,1080)).gameObject.AddComponent<HudScanlines>();scan.color=new Color(.4f,.7f,.8f,.035f);scan.raycastTarget=false;
            Frame(panel,new Vector2(1065,120),new Vector2(730,870),new Color(.17f,.36f,.41f,.55f));
            Frame(panel,new Vector2(1072,127),new Vector2(716,856),new Color(.08f,.19f,.23f,.6f));
            menuFade=panel.gameObject.AddComponent<CanvasGroup>();menuFade.alpha=0;
            Box(panel,"Rule",new Vector2(110,70),new Vector2(52,4),Accent);
            TextAt(panel,"Protocol","NEURAL FLIGHT // PROTOCOL 001",new Vector2(180,57),new Vector2(700,30),18,Accent);
            title=Rect(panel,"Title",new Vector2(116,132),new Vector2(850,258)).gameObject.AddComponent<BlockTitle>();title.color=Color.white;title.raycastTarget=false;
            menuSub=TextAt(panel,"Tagline","120 SECONDS. ONE CORE.",new Vector2(116,415),new Vector2(850,48),31,Accent);
            summary=TextAt(panel,"Brief","01  CROSS THE FIREWALL\n02  REFLECT THE CYAN SHOTS\n03  DESTROY THE WARDEN",new Vector2(116,490),new Vector2(850,150),24,new Color(.76f,.84f,.87f));
            difficultyButton=ButtonAt("Difficulty",new Vector2(116,639),new Vector2(650,60),false,()=>Game.TrySelectDifficulty(((int)Game.SelectedDifficulty+1)%3),out difficultyText);
            ButtonAt("Begin",new Vector2(116,723),new Vector2(650,96),true,()=>{if(Game.State.Phase==RunPhase.Paused)Game.TogglePause();else Game.BeginRun();},out startText);
            TextAt(panel,"EnterHelp","ENTER TO LAUNCH  /  A-D STEER  /  SPACE PARRY",new Vector2(116,838),new Vector2(850,30),17,Muted);
            ButtonAt("Mode",new Vector2(116,916),new Vector2(650,56),false,()=>{inputMode=(inputMode+1)%3;Game.Bridge.JoystickMode=inputMode==1;Game.Bridge.SetDevicesEnabled(inputMode!=0);},out modeText);
            Ring(panel,"Orbit",new Vector2(1222,151),new Vector2(482,482),new Color(.06f,.22f,.28f,.7f),1);
            heroRing=Ring(panel,"Scanner",new Vector2(1240,169),new Vector2(446,446),new Color(.12f,.67f,.76f,.7f),3);heroRing.Fill=.73f;
            heroRing.rectTransform.pivot=new Vector2(.5f,.5f);heroRing.rectTransform.anchoredPosition=new Vector2(1463,-392);
            Ring(panel,"CoreOrbit",new Vector2(1270,199),new Vector2(386,386),new Color(.07f,.35f,.4f,.65f),1);
            TextAt(panel,"Emblem","<color=#38EBFF>+</color>\nWARDEN\nPROTOCOL",new Vector2(1190,267),new Vector2(550,245),48,Color.white,TextAnchor.MiddleCenter);
            TextAt(panel,"Signal","SIGNAL ACQUIRED  //  120 BPM",new Vector2(1110,665),new Vector2(700,30),18,Muted,TextAnchor.MiddleCenter);
            ButtonAt("Effects",new Vector2(1100,751),new Vector2(310,62),false,()=>{Game.ReducedEffects=!Game.ReducedEffects;Game.SaveOptions();},out fxText);
            ButtonAt("Audio",new Vector2(1434,751),new Vector2(310,62),false,()=>Game.ToggleMute(),out audioText);
            ButtonAt("Shake",new Vector2(1100,837),new Vector2(310,62),false,()=>{Game.Shake=!Game.Shake;Game.SaveOptions();},out shakeText);
            ButtonAt("Flash",new Vector2(1434,837),new Vector2(310,62),false,()=>{Game.FlashEffects=!Game.FlashEffects;Game.SaveOptions();},out flashText);
            TextAt(panel,"OptionsHelp","F1 EFFECTS / F2 SHAKE / F3 FLASH / M AUDIO",new Vector2(1100,932),new Vector2(700,50),15,Muted);
            TextAt(panel,"Version","CYBER//DIVE   /   VECTOR ARCADE",new Vector2(116,1020),new Vector2(1600,26),15,new Color(.25f,.4f,.45f));
        }
        UnityEngine.UI.Button ButtonAt(string name,Vector2 position,Vector2 size,bool primary,UnityEngine.Events.UnityAction action,out Text label)
        {
            Box(panel,name+"Depth",position+new Vector2(5,6),size,new Color(.06f,.21f,.24f,.65f));
            var background=Box(panel,name,position,size,primary?Accent:new Color(.025f,.075f,.095f,.94f));background.raycastTarget=true;
            Frame(background.rectTransform,Vector2.one*4,size-Vector2.one*8,new Color(.35f,.65f,.7f,.3f));
            var button=background.gameObject.AddComponent<UnityEngine.UI.Button>();button.targetGraphic=background;
            var colors=button.colors;colors.highlightedColor=new Color(.72f,.95f,1);colors.pressedColor=new Color(.35f,.65f,.75f);colors.disabledColor=new Color(.45f,.5f,.53f);colors.fadeDuration=.1f;button.colors=colors;
            var navigation=button.navigation;navigation.mode=Navigation.Mode.None;button.navigation=navigation;button.onClick.AddListener(action);
            Box(background.rectTransform,"Edge",Vector2.zero,new Vector2(3,size.y),primary?Color.white:new Color(.1f,.36f,.42f));
            label=TextAt(background.rectTransform,"Label",primary?"BEGIN BREACH  >":"",new Vector2(20,0),new Vector2(size.x-40,size.y),primary?32:20,primary?new Color(.005f,.04f,.055f):Color.white,TextAnchor.MiddleCenter);return button;
        }
        RectTransform Card(RectTransform parent,string name,Vector2 position,Vector2 size)
        {
            Box(parent,name+"Depth",position+new Vector2(5,6),size,new Color(.04f,.17f,.2f,.6f));
            var card=Box(parent,name,position,size,Ink).rectTransform;Box(card,"Edge",Vector2.zero,new Vector2(32,3),Accent);
            Frame(card,new Vector2(5,5),size-new Vector2(10,10),new Color(.18f,.33f,.39f,.4f));
            Box(card,"Corner",new Vector2(size.x-32,size.y-3),new Vector2(32,3),new Color(.15f,.4f,.45f));return card;
        }
        void Frame(RectTransform parent,Vector2 position,Vector2 size,Color color)
        {
            Box(parent,"FrameTop",position,new Vector2(size.x,1),color);Box(parent,"FrameBottom",position+new Vector2(0,size.y-1),new Vector2(size.x,1),color);
            Box(parent,"FrameLeft",position,new Vector2(1,size.y),color);Box(parent,"FrameRight",position+new Vector2(size.x-1,0),new Vector2(1,size.y),color);
        }
        RectTransform Rect(RectTransform parent,string name,Vector2 position,Vector2 size)
        {
            var o=new GameObject(name,typeof(RectTransform));o.transform.SetParent(parent,false);var r=o.GetComponent<RectTransform>();
            r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(position.x,-position.y);r.sizeDelta=size;return r;
        }
        Image Box(RectTransform parent,string name,Vector2 position,Vector2 size,Color color)
        {var r=Rect(parent,name,position,size);var image=r.gameObject.AddComponent<UnityEngine.UI.Image>();image.color=color;image.raycastTarget=false;return image;}
        Text TextAt(RectTransform parent,string name,string text,Vector2 position,Vector2 size,int fontSize,Color color,TextAnchor alignment=TextAnchor.UpperLeft)
        {
            var r=Rect(parent,name,position,size);var t=r.gameObject.AddComponent<UnityEngine.UI.Text>();t.font=font;t.fontSize=fontSize;t.text=text;t.color=color;t.alignment=alignment;t.raycastTarget=false;t.verticalOverflow=VerticalWrapMode.Overflow;return t;
        }
        HudRing Ring(RectTransform parent,string name,Vector2 position,Vector2 size,Color color,float width)
        {var r=Rect(parent,name,position,size);var ring=r.gameObject.AddComponent<HudRing>();ring.color=color;ring.Thickness=width;ring.raycastTarget=false;return ring;}
        void Update()
        {
            if(!score || Game==null)return;var s=Game.State;
            if(Keyboard.current!=null && Keyboard.current.f4Key.wasPressedThisFrame)showTelemetry=!showTelemetry;
            bool playing=s.Phase==RunPhase.Playing,paused=s.Phase==RunPhase.Paused,locked=playing||paused;
            flight.gameObject.SetActive(locked);panel.gameObject.SetActive(!playing);menuFade.alpha=playing?0:Mathf.MoveTowards(menuFade.alpha,1,Time.unscaledDeltaTime*5);
            flash.color=new Color(.3f,.9f,1,Game.FlashEffects?Game.ScreenPulse*.12f:0);var difficulty=locked?Game.RunDifficulty:Game.SelectedDifficulty;
            score.text=s.Score.ToString("000000");best.text=$"{difficulty.ToString().ToUpperInvariant()} / BEST ALL {Game.BestScore:000000}";
            int seconds=Mathf.CeilToInt(Mathf.Max(0,MissionRules.Duration-Game.Elapsed));timer.text=$"{seconds/60:00}:{seconds%60:00}";timer.color=seconds<20?new Color(1,.5f,.25f):Color.white;
            sector.text=Game.SectorName;bossCaption.text=Game.BossVisible?$"WARDEN CORE / {Mathf.CeilToInt(100f*Game.BossHealth/MissionRules.BossMaxHealth)}%":"APPROACH VECTOR / CORE SIGNAL INBOUND";
            bossFill.rectTransform.sizeDelta=new Vector2(700*(Game.BossVisible?Game.BossHealth/(float)MissionRules.BossMaxHealth:Mathf.Clamp01(Game.Elapsed/60)),12);
            objective.text=Game.BossVisible?"DESTROY THE CORE":Game.Elapsed<20?"BREACH THE OUTER DEFENSE":"FIND THE GAP. KEEP MOVING.";
            for(int i=0;i<3;i++)hull[i].color=i<s.Hull?Color.white:new Color(.4f,.55f,.6f,.2f);
            charge.Fill=Mathf.Clamp01(1-(float)s.CooldownRemaining/2);charge.color=s.ShieldActive?Color.white:Accent;
            parryValue.text=s.ShieldActive?"ACTIVE":s.CooldownRemaining<=0?"READY":s.CooldownRemaining.ToString("0.0");
            syncFill.rectTransform.sizeDelta=new Vector2(342*(s.OverdriveActive?1:s.Sync/100f),10);syncValue.text=$"x{s.Multiplier:00}";
            syncCaption.text=s.OverdriveActive?"OVERDRIVE // DOUBLE FIRE":$"CHARGE {s.Sync:000}% / REFLECT TO BUILD";
            feedback.text=Game.Warning && playing?$"PARRY IN {Game.ParryArrival:0.0}s":Game.Feedback;feedback.color=Game.Feedback=="HULL DAMAGE"?new Color(1,.55f,.3f):Accent;
            telemetry.gameObject.SetActive(showTelemetry);telemetry.text=$"FRAME {Game.FrameMs:0.0}ms\nESP32 {(Game.Bridge.ControllerFresh?"LINKED":"OFFLINE")}\nRASPI {(Game.Bridge.PoseFresh?"LINKED":"OFFLINE")}\nPOSE {Game.Bridge.Gesture}";
            difficultyButton.interactable=!locked;difficultyText.text=$"{difficulty.ToString().ToUpperInvariant()} / {(locked?"LOCKED THIS RUN":"CHANGE DIFFICULTY >")}";
            audioText.text="AUDIO / "+(Game.Muted?"MUTED":"ON");fxText.text="EFFECTS / "+(Game.ReducedEffects?"LOW":"FULL");shakeText.text="SHAKE / "+(Game.Shake?"ON":"OFF");flashText.text="FLASH / "+(Game.FlashEffects?"ON":"OFF");
            modeText.text=inputMode==0?"INPUT / KEYBOARD >":inputMode==1?"INPUT / ESP32 JOYSTICK + PI >":"INPUT / ESP32 FSR + PI >";
            heroRing.rectTransform.localEulerAngles=new Vector3(0,0,-Time.unscaledTime*6);
            if(s.Phase==RunPhase.Ready)return;
            title.Caption=s.Phase==RunPhase.Won?"BREACH\nCOMPLETE":s.Phase==RunPhase.Lost?"GAME\nOVER":"FLIGHT\nPAUSED";
            menuSub.text=paused?"TAKE A BREATH. STAY SHARP.":s.Phase==RunPhase.Won?"WARDEN CORE / OFFLINE":"REBOOT. ADAPT. TRY AGAIN.";
            summary.text=$"SCORE {s.Score:000000}\n{s.Parries:00} PARRIES / {s.Hits} HULL HITS\nMISSION TIME {Game.Elapsed:0.0}s";startText.text=paused?"RESUME FLIGHT >":"REBOOT MISSION >";
        }
    }
}
