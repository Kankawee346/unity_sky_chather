using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class CatchLabUIValidation
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    [MenuItem("CatchLab/Validate Knight UI (Play Mode)")]
    public static void Validate()
    {
        var game = UnityEngine.Object.FindAnyObjectByType<CatchGame>();
        if (game == null || !EditorApplication.isPlaying) throw new InvalidOperationException("Enter Play mode first.");
        var ui = game.GetComponent<CatchLabUI>();
        Assert(Resources.Load<Texture2D>("Art/KnightRun")!=null,"four-frame running artwork loaded");
        bool originalMode = game.Joystick;
        for (int level=0; level<3; level++)
        {
            game.StartRound(level);
            Assert(game.Playing && game.Remaining==30 && game.Score==0, "round initialization");
            var player=(RectTransform)Get(game,"basket");
            var item=(RectTransform)Get(game,"item");
            float previousFall=0;
            for(int n=0;n<5;n++)
            {
                item.anchoredPosition=new Vector2(player.anchoredPosition.x,-214);
                Invoke(game,"MoveItem",.1f);
                Assert(game.Combo==n+1 && game.Caught==n+1 && game.Seen==n+1,"catch and combo");
                Assert(game.Score>=game.Caught && game.Score<=game.Caught*3,"score bounds");
            }
            item.anchoredPosition=new Vector2(390,-214); player.anchoredPosition=new Vector2(-390,-240);
            Invoke(game,"MoveItem",.1f);
            Assert(game.Combo==0 && game.Seen==6 && game.Caught==5,"miss resets combo");
            Assert(game.Hearts==4,"miss removes one heart");
            for(int miss=0;miss<6;miss++) {
                item.anchoredPosition=new Vector2(390,-214); Invoke(game,"MoveItem",.1f);
            }
            Assert(game.Hearts==0 && game.Playing && game.Remaining==30,"zero hearts do not end round");
            var kinds=new System.Collections.Generic.HashSet<int>();
            for(int n=0;n<100;n++) { Invoke(game,"SpawnItem"); kinds.Add(game.RelicKind); }
            Assert(kinds.Count==3,"three relic types");
            game.SelectSensor(false); player.anchoredPosition=new Vector2(0,-240);
            game.ReceiveSensorSample(true,false,1); Invoke(game,"MoveBasket",.1f);
            Assert(player.anchoredPosition.x<0,"FSR left ignores joystick right");
            game.ReceiveSensorSample(true,true,0); float x=player.anchoredPosition.x; Invoke(game,"MoveBasket",.1f);
            Assert(player.anchoredPosition.x==x,"simultaneous inputs stop");
            game.SelectSensor(true); player.anchoredPosition=new Vector2(0,-240);
            game.ReceiveSensorSample(true,false,1); Invoke(game,"MoveBasket",.1f);
            Assert(player.anchoredPosition.x>0,"joystick right ignores FSR left");
            Set(game,"sampleTime",float.NegativeInfinity); x=player.anchoredPosition.x; Invoke(game,"MoveBasket",.1f);
            Assert(player.anchoredPosition.x==x,"stale sensor stops");
            item.anchoredPosition=new Vector2(0,183); Invoke(game,"MoveItem",.1f);
            previousFall=183-item.anchoredPosition.y;
            Assert(Mathf.Abs(previousFall-new[]{14.5f,27.5f,42.5f}[level])<.01f,"three fall speeds");
            game.TogglePause(); float remaining=game.Remaining; Invoke(game,"Update");
            Assert(game.Remaining==remaining,"pause timer"); game.TogglePause();
            ui.ShowResults(); ui.Tick();
            var countText=Array.Find(UnityEngine.Object.FindObjectsByType<TMPro.TextMeshProUGUI>(),t=>t.text=="5/12");
            Assert(countText!=null,"result caught/total exists");
            countText.ForceMeshUpdate();
            Assert(countText.textInfo.lineCount==1,"caught/total stays on one line");
        }
        game.LeaveRound(); game.SelectSensor(originalMode); ui.Home();
        Invoke(ui,"ToggleLanguage"); Invoke(ui,"ToggleLanguage");
        ui.Setup(); Invoke(ui,"Instructions"); ui.Home();
        foreach(var text in UnityEngine.Object.FindObjectsByType<TMPro.TextMeshProUGUI>())
        {
            text.ForceMeshUpdate();
            Assert(text.font!=null,"font assigned");
            Assert(!text.isTextOverflowing,"text fits: "+text.text);
        }
        Assert(UnityEngine.Object.FindObjectsByType<UnityEngine.EventSystems.EventSystem>().Length==1,"one EventSystem");
        System.IO.Directory.CreateDirectory("Logs");
        System.IO.File.WriteAllText("Logs/KnightUI-validation.txt", "PASS: three speeds, scoring, three relic types, heart loss, zero hearts continues, catches, combo, miss reset, mode isolation, stale sensor timeout, pause, navigation, bilingual font and EventSystem. No cloud rounds written.\n"+DateTime.UtcNow.ToString("O"));
        Debug.Log("CatchLab Knight UI validation PASS. No cloud rounds written.");
    }
    private static object Get(object target,string name)=>target.GetType().GetField(name,Private).GetValue(target);
    private static void Set(object target,string name,object value)=>target.GetType().GetField(name,Private).SetValue(target,value);
    private static void Invoke(object target,string name,params object[] args)=>target.GetType().GetMethod(name,Private).Invoke(target,args);
    private static void Assert(bool passed,string label) { if(!passed) throw new Exception("UI validation failed: "+label); }
}
