using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

// Presentation only: CatchGame remains responsible for movement, catches and scoring.
public sealed class CatchLabUI : MonoBehaviour
{
    private CatchGame game;
    private Transform root;
    private RectTransform page, arena;
    private TMP_FontAsset font;
    private TMP_FontAsset pixelFont;
    private bool thai;
    private int difficulty;
    private string currentPage;
    private readonly List<Action> translations = new List<Action>();
    private readonly Color ink = new Color32(16, 13, 22, 245);
    private readonly Color paper = new Color32(255, 229, 185, 255);
    private Color Accent => difficulty == 0 ? new Color32(221, 180, 111, 255) : difficulty == 1 ? new Color32(244, 155, 71, 255) : new Color32(255, 105, 66, 255);
    private TextMeshProUGUI hudScore, hudTime, hudCombo, hudConnection, saveLabel, pauseLabel, relicLabel;
    private RectTransform timerFill, knight;
    private TextMeshProUGUI popup;
    private readonly List<CatchLabPixelIcon> heartIcons = new List<CatchLabPixelIcon>();
    private CatchLabPixelIcon relicIcon;
    private UnityEngine.UI.RawImage knightArt, backdrop;
    private Texture2D idleKnight, runKnight;
    private UnityEngine.UI.Image damageFlash;
    private readonly List<RectTransform> embers = new List<RectTransform>();
    private float visualTime, catchImpact, damageImpact, popupLife, lastPlayerX;
    private UnityEngine.UI.Image relic;
    private GameObject pausePanel;
    private Coroutine analyticsRequest;
    private TextMeshProUGUI analyticsStatus;
    private RectTransform analyticsBody;
    private Analytics analytics;
    private int analyticsGeneration;
    private readonly List<UnityEngine.UI.Image> levelBorders = new List<UnityEngine.UI.Image>();
    private readonly List<UnityEngine.UI.Image> modeBorders = new List<UnityEngine.UI.Image>();
    private readonly string[] levelEn = { "ASHEN GROVE", "EMBER KEEP", "CINDER CROWN" };
    private readonly string[] levelTh = { "พงไพรเถ้าธุลี", "ปราการคุไฟ", "มงกุฎเพลิง" };

    public RectTransform Build(CatchGame owner, Transform canvas)
    {
        game = owner; root = canvas;
        thai = PlayerPrefs.GetInt("CatchLab.Thai", 1) == 1;
        Font source = Resources.Load<Font>("Art/ChakraPetch-Regular");
        font = TMP_FontAsset.CreateFontAsset(source, 48, 6, UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 2048, 2048);
        font.isMultiAtlasTexturesEnabled = true;
        pixelFont = TMP_FontAsset.CreateFontAsset(Resources.Load<Font>("Art/PressStart2P-Regular"), 32, 4, UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 1024, 1024);
        pixelFont.fallbackFontAssetTable = new List<TMP_FontAsset> { font };
        backdrop = Raw(root, "Backdrop", "Sanctuary", 1440, 900, 0, 0, new Color(.48f, .40f, .47f));
        Stretch(backdrop.rectTransform);
        Stretch(Box(root, "Shade", 1440, 900, 0, 0, new Color(0.035f, .02f, .035f, .50f)).rectTransform);
        page = Rect(root, "Pages", 1440, 900, 0, 0);
        arena = Rect(root, "Arena", 960, 600, 0, -70);
        arena.SetSiblingIndex(page.GetSiblingIndex());
        for(int i=0;i<24;i++)
            embers.Add(Box(arena,"Ember",i%3==0?4:2,i%3==0?4:2,0,0,new Color(1,.55f,.15f,.65f)).rectTransform);
        arena.gameObject.SetActive(false);
        var overlay = Rect(root, "CRT", 1440, 900, 0, 0);
        for (int y = -448; y < 450; y += 5)
            Box(overlay, "Scanline", 1440, 1, 0, y, new Color(0,0,0,.065f));
        return arena;
    }

    public void DressPlayer(RectTransform player, UnityEngine.UI.Image falling)
    {
        player.GetComponent<UnityEngine.UI.Image>().color = Color.clear;
        knightArt = Raw(player, "Knight", "Knight", 116, 116, 0, -42, Color.white);
        knight = knightArt.rectTransform;
        idleKnight=knightArt.texture as Texture2D;
        runKnight=Resources.Load<Texture2D>("Art/KnightRun");
        relic = falling;
        relic.color=Color.clear;
        relicIcon=Icon(falling.transform,0,40,0,0,Color.white);
        relicLabel = Text(falling.transform, "1", 48, 20, 0, -28, 12, paper);
        falling.raycastTarget = false;
    }

    public void SetRelic(int kind, int points)
    {
        relic.color=Color.clear;
        relicIcon.Kind=kind;
        relicLabel.text = "+"+points;
        relic.rectTransform.localRotation=Quaternion.identity;
    }

    public void Home()
    {
        game.LeaveRound(); Begin("home");
        Label(page, "FIELD NOTES / 01", "บันทึกการฝึก / 01", 480, 32, -360, 255, 18);
        Text(page, "CATCH\nLAB", 610, 230, -300, 112, 96, paper, TextAlignmentOptions.Left);
        Label(page, "THE EMBER WATCH", "ผู้พิทักษ์ประกายไฟ", 520, 50, -340, -38, 29);
        Label(page, "A steady hand. A brighter flame.\nTrain your timing. Collect every relic.", "ฝึกสองมือให้มั่นคง ปกป้องประกายไฟ\nเคลื่อนอัศวินเพื่อรับวัตถุและทำคะแนน", 520, 90, -340, -118, 21);
        Button(page, "BEGIN TRAINING  >", "เริ่มการฝึก  >", -360, -225, 480, 65, Setup, true);
        Button(page, "HOW TO PLAY", "วิธีเล่น", -485, -310, 230, 52, Instructions);
        Button(page, "YOUR RECORDS", "ประวัติผล", -235, -310, 230, 52, ShowAnalytics);
        Frame(page, 420, 590, 350, 0, Accent);
        Label(page, "THE RELIC WARDEN", "อัศวินผู้พิทักษ์", 360, 40, 350, 245, 24);
        Raw(page, "KnightPortrait", "Knight", 350, 350, 350, 15, Color.white);
        Label(page, "SHIELD READY   /   30 SEC", "พร้อมรับวัตถุ   /   30 วินาที", 360, 40, 350, -210, 18);
        Footer();
    }

    private void Begin(string id)
    {
        if (analyticsRequest != null) { StopCoroutine(analyticsRequest); analyticsRequest = null; }
        analyticsGeneration++;
        translations.Clear(); levelBorders.Clear(); modeBorders.Clear();
        foreach (Transform child in page) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
        currentPage = id; arena.gameObject.SetActive(id == "play");
        page.anchoredPosition=Vector2.zero;
        backdrop.color = new Color(.48f,.40f,.47f);
        hudScore = hudTime = hudCombo = hudConnection = saveLabel = pauseLabel = null;
        if(id=="play") return;
        Box(page, "TopRule", 1320, 3, 0, 358, Accent * new Color(1,1,1,.6f));
        Text(page, "C / L     CATCHLAB", 460, 45, -430, 400, 23, Accent, TextAlignmentOptions.Left);
        Button(page, "TH / EN", "ไทย / EN", 560, 400, 170, 42, ToggleLanguage);
    }

    private void ToggleLanguage()
    {
        thai = !thai; PlayerPrefs.SetInt("CatchLab.Thai", thai ? 1 : 0); PlayerPrefs.Save();
        foreach (var action in translations) action();
        if (currentPage == "records" && analytics != null) RenderAnalytics();
        Tick();
    }

    public void Setup()
    {
        Begin("setup");
        Label(page, "CHOOSE YOUR TRIAL", "เลือกระดับการฝึก", 1000, 65, 0, 300, 39);
        Label(page, "Three trials. One guardian. Grow at your own pace.", "สามระดับความท้าทาย เลือกความยากที่เหมาะกับคุณ", 1100, 40, 0, 245, 20);
        for (int i=0; i<3; i++)
        {
            int index=i; float x=(i-1)*420;
            var card=Box(page, "Trial"+i, 390, 320, x, 35, ink);
            levelBorders.Add(Frame(card.transform, 390, 320, 0, 0, Accent));
            Text(card.transform, "0"+(i+1), 330, 75, 0, 100, 52, Accent);
            Label(card.transform, levelEn[i], levelTh[i], 350, 45, 0, 32, 26);
            string[] en={"EASY / Gentle amber\nSlow fall · predictable lanes", "MEDIUM / Warming embers\nFaster fall · random + drifting", "HARD / Burning crown\nVery fast · zigzag drift"};
            string[] th={"ง่าย / แสงอำพันนุ่ม\nตกช้า · ตำแหน่งคาดเดาง่าย", "กลาง / คุไฟส้ม\nตกเร็วขึ้น · สุ่มตำแหน่งและไหลเฉียง", "ยาก / เพลิงแดง\nตกเร็วมาก · ส่ายซ้ายขวา"};
            Label(card.transform, en[i], th[i], 350, 80, 0, -42, 20);
            Button(card.transform, "SELECT", "เลือก", 0, -120, 330, 45, ()=> { difficulty=index; game.SelectDifficulty(index); UpdateSelections(); });
        }
        Label(page, "CONTROL METHOD", "เลือกวิธีควบคุม", 340, 40, -450, -180, 20);
        modeBorders.Add(Button(page, "FSR  /  A + D", "FSR / ปุ่ม A + D", -100, -180, 330, 52, ()=> { game.SelectSensor(false); UpdateSelections(); }));
        modeBorders.Add(Button(page, "JOYSTICK  /  ARROWS", "จอยสติก / ปุ่มลูกศร", 290, -180, 330, 52, ()=> { game.SelectSensor(true); UpdateSelections(); }));
        Button(page, "<  BACK", "<  กลับ", -465, -310, 300, 58, Home);
        Button(page, "START 30-SECOND ROUND  >", "เริ่มเล่น 30 วินาที  >", 230, -310, 760, 65, ()=>game.StartRound(difficulty), true);
        UpdateSelections(); Footer();
    }

    private void UpdateSelections()
    {
        for(int i=0;i<levelBorders.Count;i++) TintBorder(levelBorders[i],i==difficulty ? Accent : new Color(.35f,.29f,.25f,.5f));
        for(int i=0;i<modeBorders.Count;i++) TintBorder(modeBorders[i],(i==1)==game.Joystick ? Accent : new Color(.35f,.29f,.25f,.5f));
    }

    private static void TintBorder(UnityEngine.UI.Image edge,Color color)
    {
        edge.color=color;
        edge.transform.parent.GetComponent<CatchLabFrame>().color=color;
    }

    public void ShowPlay()
    {
        difficulty=game.Level; Begin("play");
        backdrop.color = difficulty==0 ? new Color(.93f,.85f,.85f) : difficulty==1 ? new Color(.88f,.65f,.53f) : new Color(.68f,.37f,.38f);
        Box(page,"HUD",1440,142,0,379,new Color(.055f,.035f,.08f,.93f));
        Box(page,"HUDRule",1440,4,0,308,Accent);
        Label(page,"SCORE", "คะแนน",250,25,-550,422,16);
        hudScore=Text(page,"000000",270,47,-550,382,31,paper);
        heartIcons.Clear();
        for(int i=0;i<5;i++) heartIcons.Add(Icon(page,4,26,-642+i*37,337,Color.white));
        Label(page,levelEn[difficulty],levelTh[difficulty],370,32,-170,422,18);
        hudCombo=Text(page,"",370,42,-170,380,22,Accent);
        hudConnection=Text(page,"",370,27,-170,337,14,paper);
        Label(page,"CATCH THE RELICS", "รับวัตถุให้ทัน",300,28,190,411,18);
        Label(page,"THREE TRIALS  ·  30 SECONDS", "สามระดับ  ·  30 วินาที",300,28,190,375,15);
        hudTime=Text(page,"30",135,55,435,399,37,Accent);
        timerFill=Box(page,"TimeFill",120,6,435,354,Accent).rectTransform;
        Button(page,"PAUSE", "พักเกม",605,411,170,48,()=>game.TogglePause());
        Button(page,"TH / EN", "ไทย / EN",605,350,170,40,ToggleLanguage);
        popup=Text(page,"",1000,95,0,220,43,paper); popupLife=0;
        damageFlash=Box(page,"DamageFlash",1440,900,0,0,Color.clear);
        damageFlash.raycastTarget=false;
        var pause=Box(page,"Pause",1440,900,0,0,new Color(.03f,.02f,.04f,.94f)); pausePanel=pause.gameObject;
        pauseLabel=Label(pause.transform,"TAKE A BREATH", "พักสักครู่",900,80,0,100,52);
        Button(pause.transform,"RESUME", "เล่นต่อ",0,-20,450,65,()=>game.TogglePause(),true);
        Button(pause.transform,"END ROUND / NO SAVE", "ออกจากรอบนี้ (ไม่บันทึก)",0,-110,450,55,Home);
        pausePanel.SetActive(false);
        catchImpact=damageImpact=0; lastPlayerX=0;
        Tick();
    }

    public void Tick()
    {
        if(currentPage=="play")
        {
            var canvasRect=(RectTransform)root;
            page.anchoredPosition=new Vector2(0,(canvasRect.rect.height-900)/2);
            float scale=Mathf.Min(canvasRect.rect.width/960f,(canvasRect.rect.height-142)/600f);
            arena.localScale=Vector3.one*scale;
            arena.anchoredPosition=new Vector2(0,-canvasRect.rect.height/2+352*scale);
            ((RectTransform)page.Find("HUD")).sizeDelta=new Vector2(canvasRect.rect.width,142);
            ((RectTransform)page.Find("HUDRule")).sizeDelta=new Vector2(canvasRect.rect.width,4);
            damageFlash.rectTransform.sizeDelta=canvasRect.rect.size;
            damageFlash.rectTransform.anchoredPosition=-page.anchoredPosition;
            ((RectTransform)pausePanel.transform).sizeDelta=canvasRect.rect.size;
            ((RectTransform)pausePanel.transform).anchoredPosition=-page.anchoredPosition;
            hudScore.text=game.Score.ToString("000000"); hudTime.text=Mathf.CeilToInt(game.Remaining).ToString("00");
            timerFill.localScale=new Vector3(Mathf.Clamp01(game.Remaining/30f),1,1);
            hudCombo.text=$"COMBO  x{game.Combo:00}";
            hudCombo.rectTransform.localScale=Vector3.one*(1+catchImpact*.18f);
            hudConnection.text = (game.Joystick?"JOY / ← →":"FSR / A D")+(game.SensorOnline?"  · LIVE":"  · KEYBOARD");
            for(int i=0;i<heartIcons.Count;i++) heartIcons[i].color=i<game.Hearts?Color.white:new Color(.22f,.17f,.23f);
            pausePanel.SetActive(game.Paused);
            if(!game.Paused) AnimatePlay();
        }
        if(currentPage=="results" && saveLabel!=null) {
            saveLabel.text=game.SaveState=="saved" ? (thai ? "บันทึกผลลง Firebase แล้ว" : "ROUND SAVED TO FIREBASE") : game.SaveState=="failed" ? (thai ? "บันทึกไม่สำเร็จ · ตรวจการเชื่อมต่อ Bridge" : "SAVE FAILED · Check bridge connection") : (thai ? "กำลังบันทึกผล…" : "SAVING YOUR ROUND…");
            saveLabel.font=ChooseFont(saveLabel.text);
        }
    }

    public void React(bool caught, float x, int points)
    {
        if(currentPage!="play") return;
        if(caught) catchImpact=1; else damageImpact=1;
        string[] praiseTh={"", "", "ใช้ได้", "ดี", "ดีมาก", "ยอดเยี่ยม"};
        popupLife=1.2f;
        bool milestone=caught && game.Combo>0 && game.Combo%20==0;
        popup.text=caught ? (milestone?$"{game.Combo} COMBO!":game.Combo>=2?(thai?praiseTh[Mathf.Min(game.Combo,5)]:game.PraiseText):$"+{points}") : (thai?"พลาด!  -1 หัวใจ":"MISS!  -1 HEART");
        popup.color=caught?Accent:new Color32(255,96,82,255);
        popup.font=ChooseFont(popup.text);
        for(int i=0;i<8;i++)
        {
            var spark=Box(arena,"Impact",5,5,x,-216,caught?Accent:new Color(1,.2f,.12f));
            var motion=spark.gameObject.AddComponent<CatchLabSpark>();
            motion.Configure(game,new Vector2(Mathf.Cos(i*Mathf.PI/4)*90,Mathf.Sin(i*Mathf.PI/4)*90+30));
        }
    }

    private void AnimatePlay()
    {
        float dt=Time.unscaledDeltaTime; visualTime+=dt;
        catchImpact=Mathf.MoveTowards(catchImpact,0,dt*4);
        damageImpact=Mathf.MoveTowards(damageImpact,0,dt*3);
        popupLife=Mathf.Max(0,popupLife-dt);
        popup.alpha=Mathf.Clamp01(popupLife*3);
        popup.rectTransform.localScale=Vector3.one*(1+Mathf.Sin(Mathf.Clamp01((1.2f-popupLife)*4)*Mathf.PI)*.14f);
        popup.rectTransform.anchoredPosition=new Vector2(0,220+(1.2f-popupLife)*14);
        damageFlash.color=new Color(.8f,.03f,.04f,damageImpact*.22f);
        float x=((RectTransform)knight.parent).anchoredPosition.x;
        bool walking=Mathf.Abs(x-lastPlayerX)>.05f;
        if(walking && runKnight!=null) {
            knightArt.texture=runKnight;
            knightArt.uvRect=new Rect((Mathf.FloorToInt(visualTime*11)%4)*.25f,0,.25f,1);
        } else {
            knightArt.texture=idleKnight;
            knightArt.uvRect=new Rect(0,0,1,1);
        }
        knight.anchoredPosition=new Vector2(Mathf.Round(Mathf.Sin(visualTime*40)*damageImpact*3),-42+Mathf.Round(Mathf.Sin(visualTime*(walking?16:3))*(walking?3:1))-catchImpact*5);
        knight.localScale=new Vector3(1+catchImpact*.08f,1-catchImpact*.12f,1);
        knight.localRotation=Quaternion.Euler(0,0,walking?Mathf.Sin(visualTime*16)*3:0);
        knightArt.color=damageImpact>.2f?new Color(1,.4f,.35f):Color.Lerp(Color.white,new Color(1,1,.6f),catchImpact);
        lastPlayerX=x;
        relicIcon.rectTransform.localRotation=Quaternion.Euler(0,0,Mathf.Round(Mathf.Sin(visualTime*3)*12));
        for(int i=0;i<embers.Count;i++)
        {
            float y=Mathf.Repeat(i*31+visualTime*(12+i%4*5),580)-290;
            embers[i].anchoredPosition=new Vector2(Mathf.Round(-450+Mathf.Repeat(i*137,900)+Mathf.Sin(visualTime+i)*8),Mathf.Round(y));
            embers[i].localScale=Vector3.one*(Mathf.Sin(visualTime*3+i)>0?1:0.5f);
        }
    }

    public void ShowResults()
    {
        Begin("results");
        Label(page,"TRIAL COMPLETE", "จบการฝึก",1100,65,0,285,44);
        Label(page,levelEn[game.Level],levelTh[game.Level],1000,40,0,230,24);
        Frame(page,1100,290,0,20,Accent);
        Text(page,game.Score.ToString("000"),350,115,-330,65,88,paper);
        Label(page,"SCORE", "คะแนน",300,45,-330,-30,22);
        Text(page,game.Seen>0 ? (100f*game.Caught/game.Seen).ToString("0.#")+"%" : "—",350,115,0,65,68,Accent);
        Label(page,"CATCH ACCURACY", "อัตรารับสำเร็จ",350,45,0,-30,22);
        var caughtCount=Text(page,$"{game.Caught}/{game.Seen}",350,105,330,65,43,paper);
        caughtCount.textWrappingMode=TextWrappingModes.NoWrap;
        Label(page,"CAUGHT / TOTAL", "รับได้ / ทั้งหมด",350,45,330,-30,20);
        Label(page,"30 seconds  ·  Accuracy measures catches, score measures relic value.", "30 วินาที · ความแม่นยำคิดจากจำนวนที่รับได้ คะแนนคิดตามมูลค่าวัตถุ",1050,45,0,-92,19);
        saveLabel=Text(page,"",1100,50,0,-185,22,Accent);
        Button(page,"PLAY AGAIN", "เล่นอีกครั้ง",-400,-285,350,62,()=>game.StartRound(game.Level),true);
        Button(page,"YOUR RECORDS", "ดูประวัติผล",0,-285,350,62,ShowAnalytics);
        Button(page,"MAIN MENU", "เมนูหลัก",400,-285,350,62,Home);
        Footer(); Tick();
    }

    private void Instructions()
    {
        Begin("help");
        Label(page,"THE WARDEN'S GUIDE", "คู่มือผู้พิทักษ์",1100,70,0,285,40);
        Frame(page,1200,440,0,-5,Accent);
        Label(page,
            "01  MOVE & CATCH\nCoin +1 / Crystal +2 / Potion +3.\n\n02  CONTROLS\nFSR: pads or A / D. Joystick: stick or arrow keys.\nBoth directions stop. Only the selected sensor controls movement.\n\n03  STREAK\nPraise at 2 / 3 / 4 / 5 catches. Milestone every 20.\nMiss: -1 heart and reset combo. Even at 0 hearts, finish all 30 seconds.",
            "01  เคลื่อนที่และรับวัตถุ\nเหรียญ +1 / อัญมณี +2 / ขวดยา +3 คะแนน\n\n02  ควบคุมได้สองรูปแบบ\nFSR: แผ่นกดซ้าย / ขวา หรือ A / D   จอย: โยกซ้าย / ขวา หรือปุ่มลูกศร\nกดสองข้างพร้อมกันจะหยุด โหมดที่เลือกไม่รับคำสั่งจากเซนเซอร์อีกประเภท\n\n03  สะสมคอมโบ\nรับติดกัน 2 / 3 / 4 / 5 ครั้งมีคำชม และเอฟเฟกต์ทุก 20 คอมโบ\nพลาดเสีย 1 หัวใจและคอมโบเริ่มใหม่ หัวใจหมดก็เล่นต่อจนครบ 30 วินาที",
            1100,420,0,-5,23);
        Button(page,"< MAIN MENU", "< เมนูหลัก",-320,-310,500,60,Home);
        Button(page,"CHOOSE YOUR TRIAL >", "เลือกระดับการฝึก >",320,-310,500,60,Setup,true);
        Footer();
    }

    private void ShowAnalytics()
    {
        Begin("records");
        Label(page,"YOUR TRAINING RECORD", "บันทึกผลการฝึก",1100,60,0,300,38);
        analyticsStatus=Text(page,thai?"กำลังโหลดข้อมูล…":"Loading records…",1150,60,0,235,20,paper);
        analyticsBody=Rect(page,"RecordCards",1300,430,0,-10);
        Button(page,"< MAIN MENU", "< เมนูหลัก",-430,-310,360,58,Home);
        Button(page,"REFRESH", "โหลดใหม่",0,-310,360,58,ShowAnalytics);
        Button(page,"TRAIN AGAIN >", "เริ่มการฝึก >",430,-310,360,58,Setup,true);
        Label(page,"Latest 100 rounds · Compare within each level · Game performance is not a medical diagnosis.","100 รอบล่าสุด · เปรียบเทียบภายในระดับเดียวกัน · ผลเกมไม่ใช่การวินิจฉัยทางการแพทย์",1300,45,0,-390,17);
        analytics=null;
        analyticsRequest=StartCoroutine(LoadAnalytics(analyticsGeneration));
    }

    private IEnumerator LoadAnalytics(int generation)
    {
        string url=BridgeEndpoint.Resolve("/rounds/analytics");
        if(url==null) { AnalyticsError(); yield break; }
        using(var request=UnityWebRequest.Get(url))
        {
            request.timeout=12; yield return request.SendWebRequest();
            if(generation!=analyticsGeneration) yield break;
            if(request.result!=UnityWebRequest.Result.Success) { AnalyticsError(); yield break; }
            try { analytics=JsonUtility.FromJson<Analytics>(request.downloadHandler.text); }
            catch { analytics=null; }
            if(analytics==null || analytics.levels==null) { AnalyticsError(); yield break; }
            RenderAnalytics();
        }
        analyticsRequest=null;
    }

    private void AnalyticsError()
    {
        analyticsStatus.text=thai?"ยังอ่านประวัติไม่ได้ · เปิด Bridge และตรวจอินเทอร์เน็ต แล้วกดโหลดใหม่":"Unable to load records · Start the bridge, check internet, then refresh.";
    }

    private void RenderAnalytics()
    {
        foreach(Transform child in analyticsBody) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
        analyticsStatus.text=$"{(thai?"ผู้เล่น":"PLAYER")}  {analytics.playerId}     /     {analytics.roundsAnalyzed} {(thai?"รอบ":"ROUNDS")}";
        for(int i=0;i<3;i++)
        {
            Summary data=Array.Find(analytics.levels,s=>s.level==i+1);
            var card=Box(analyticsBody,"Level"+i,390,400,(i-1)*420,0,ink);
            Frame(card.transform,390,400,0,0,Accent);
            Text(card.transform,thai?levelTh[i]:levelEn[i],350,40,0,158,24,Accent);
            int count=data?.roundCount??0;
            Text(card.transform,$"{count} {(thai?"รอบ":"rounds")}",350,32,0,116,19,paper);
            var measured=new List<History>();
            if(data?.history!=null) foreach(var h in data.history) if(h.itemsSeen>0) measured.Add(h);
            Text(card.transform,measured.Count>0 ? data.accuracyPercent.ToString("0.#")+"%" : "—",350,80,0,52,48,paper);
            Text(card.transform,thai?"อัตรารับสำเร็จรวม":"TOTAL CATCH ACCURACY",350,30,0,0,16,Accent);
            string change=measured.Count<2 ? (thai?"รอข้อมูลอย่างน้อย 2 รอบ":"Need at least 2 measured rounds") : data.changePercentagePoints.ToString("+0.#;-0.#;0")+(thai?" จุดเปอร์เซ็นต์":" percentage points");
            Text(card.transform,change,360,45,0,-38,18,paper);
            var plot=Rect(card.transform,"Trend",310,80,0,-112);
            Box(plot,"Baseline",310,1,0,-40,new Color(.4f,.33f,.25f));
            if(measured.Count>0)
            {
                int start=Mathf.Max(0,measured.Count-12); int n=measured.Count-start;
                for(int p=0;p<n;p++)
                {
                    float h=measured[start+p].accuracyPercent*.8f;
                    Box(plot,"RoundBar",Mathf.Min(18,280f/n),Mathf.Max(2,h),-140+(p+.5f)*280/n,-40+h/2,Accent);
                }
            }
            Text(card.transform,thai?"รอบเก่า → ล่าสุด (สูงสุด 12 รอบ)":"OLDER → LATEST (UP TO 12)",360,30,0,-173,14,paper);
        }
    }

    private void Footer() => Label(page,"CATCHLAB  /  SENSOR TRAINING     •     THE EMBER WATCH", "CATCHLAB / เกมฝึกควบคุมด้วยเซนเซอร์     •     ผู้พิทักษ์ประกายไฟ",1250,35,0,-405,16);

    private TextMeshProUGUI Label(Transform parent,string en,string th,float w,float h,float x,float y,int size)
    {
        var label=Text(parent,thai?th:en,w,h,x,y,size,paper);
        translations.Add(()=> { if(label!=null) { label.text=thai?th:en; label.font=ChooseFont(label.text); } });
        return label;
    }

    private UnityEngine.UI.Image Button(Transform parent,string en,string th,float x,float y,float w,float h,Action action,bool primary=false)
    {
        var panel=Box(parent,"Button_"+en,w,h,x,y,primary?new Color(.28f,.16f,.09f,.97f):ink);
        panel.raycastTarget=true;
        var border=Frame(panel.transform,w,h,0,0,Accent);
        var button=panel.gameObject.AddComponent<UnityEngine.UI.Button>(); button.targetGraphic=panel;
        var colors=button.colors; colors.normalColor=Color.white; colors.highlightedColor=new Color(1.35f,1.25f,1.15f); colors.pressedColor=new Color(.7f,.7f,.7f); button.colors=colors;
        button.onClick.AddListener(()=>action());
        var label=Label(panel.transform,en,th,w-22,h-6,0,0,primary?25:21); label.color=primary?paper:Accent;
        panel.gameObject.AddComponent<CatchLabButtonFeedback>().Configure(panel, label, Accent);
        return border;
    }

    private TextMeshProUGUI Text(Transform parent,string value,float w,float h,float x,float y,int size,Color color,TextAlignmentOptions align=TextAlignmentOptions.Center)
    {
        var label=Rect(parent,"Text",w,h,x,y).gameObject.AddComponent<TextMeshProUGUI>();
        label.font=ChooseFont(value); label.fontSize=size; label.text=value; label.color=color; label.alignment=align;
        label.raycastTarget=false; label.enableAutoSizing=true; label.fontSizeMin=size*.50f; label.fontSizeMax=size;
        label.overflowMode=TextOverflowModes.Overflow; return label;
    }

    private TMP_FontAsset ChooseFont(string value)
    {
        foreach(char c in value) if(c>='\u0E00' && c<='\u0E7F') return font;
        return pixelFont;
    }

    private static CatchLabPixelIcon Icon(Transform parent,int kind,float size,float x,float y,Color tint)
    {
        var rect=Rect(parent,"PixelIcon",size,size,x,y);
        rect.gameObject.AddComponent<CanvasRenderer>();
        var icon=rect.gameObject.AddComponent<CatchLabPixelIcon>(); icon.Kind=kind; icon.color=tint; icon.raycastTarget=false; return icon;
    }

    private static RectTransform Rect(Transform parent,string name,float w,float h,float x,float y)
    {
        var rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent,false); rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);
        rect.sizeDelta=new Vector2(w,h); rect.anchoredPosition=new Vector2(x,y); return rect;
    }
    private static UnityEngine.UI.Image Box(Transform parent,string name,float w,float h,float x,float y,Color color)
    {
        var image=Rect(parent,name,w,h,x,y).gameObject.AddComponent<UnityEngine.UI.Image>(); image.color=color; image.raycastTarget=false; return image;
    }
    private static UnityEngine.UI.Image Frame(Transform parent,float w,float h,float x,float y,Color color)
    {
        var frame=Rect(parent,"Frame",w,h,x,y);
        frame.gameObject.AddComponent<CanvasRenderer>();
        // Border follows one tint through a CanvasGroup-free custom graphic.
        var graphic=frame.gameObject.AddComponent<CatchLabFrame>(); graphic.color=color; graphic.raycastTarget=false;
        // A top edge also provides a tintable reference for selected cards.
        return Box(frame,"Top",w,3,0,h/2,color);
    }
    private static UnityEngine.UI.RawImage Raw(Transform parent,string name,string asset,float w,float h,float x,float y,Color color)
    {
        var raw=Rect(parent,name,w,h,x,y).gameObject.AddComponent<UnityEngine.UI.RawImage>(); raw.texture=Resources.Load<Texture2D>("Art/"+asset); raw.color=color; raw.raycastTarget=false; return raw;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin=Vector2.zero; rect.anchorMax=Vector2.one;
        rect.offsetMin=rect.offsetMax=Vector2.zero;
    }

    [Serializable] private class Analytics { public string playerId; public int roundsAnalyzed; public Summary[] levels; }
    [Serializable] private class Summary { public int level; public int roundCount; public float accuracyPercent; public float changePercentagePoints; public History[] history; }
    [Serializable] private class History { public int itemsSeen; public float accuracyPercent; }
}

public sealed class CatchLabFrame : UnityEngine.UI.MaskableGraphic
{
    protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper vh)
    {
        vh.Clear(); Rect r=rectTransform.rect; float t=3;
        Quad(vh,r.xMin,r.yMin,r.width,t); Quad(vh,r.xMin,r.yMax-t,r.width,t);
        Quad(vh,r.xMin,r.yMin,t,r.height); Quad(vh,r.xMax-t,r.yMin,t,r.height);
    }
    private void Quad(UnityEngine.UI.VertexHelper vh,float x,float y,float w,float h)
    {
        int i=vh.currentVertCount; vh.AddVert(new Vector3(x,y),color,Vector2.zero); vh.AddVert(new Vector3(x,y+h),color,Vector2.zero);
        vh.AddVert(new Vector3(x+w,y+h),color,Vector2.zero); vh.AddVert(new Vector3(x+w,y),color,Vector2.zero);
        vh.AddTriangle(i,i+1,i+2); vh.AddTriangle(i,i+2,i+3);
    }
}
