// Verification only. Lives outside Assets and is compiled in memory by Pipeline.
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
public static class FlightVerification
{
    static CyberDive.DiveGame game;
    static Keyboard keyboard;
    static double started,lastPulse;
    static bool captured;
    static List<float> frames=new List<float>();
    static FieldInfo shots,drones;
    public static void Run()
    {
        game=UnityEngine.Object.FindAnyObjectByType<CyberDive.DiveGame>();
        if(game==null)throw new Exception("Game is not playing");
        game.BeginRun();keyboard=InputSystem.AddDevice<Keyboard>();
        shots=game.GetType().GetField("shots",BindingFlags.Instance|BindingFlags.NonPublic);
        drones=game.GetType().GetField("drones",BindingFlags.Instance|BindingFlags.NonPublic);
        started=EditorApplication.timeSinceStartup;lastPulse=-10;captured=false;frames.Clear();
        EditorApplication.update-=Tick;EditorApplication.update+=Tick;
    }
    static void Tick()
    {
        if(!game || !keyboard.added){Finish("aborted");return;}
        if(game.State.Phase!=CyberDive.RunPhase.Playing){Finish(game.State.Phase.ToString());return;}
        if(EditorApplication.timeSinceStartup-started>135){Finish("timeout");return;}
        frames.Add(Time.unscaledDeltaTime*1000);
        if(!captured && game.Elapsed>65){captured=true;Directory.CreateDirectory("Screenshots");ScreenCapture.CaptureScreenshot("Screenshots/cyber-dive-options-1080p.png");}
        float x=game.PilotX,target=game.BossVisible?Mathf.Sin(game.Elapsed*.55f)*3:0;
        bool pulse=false;
        foreach(var shot in (IEnumerable)shots.GetValue(game))
        {
            var type=shot.GetType();if(!(bool)type.GetField("live").GetValue(shot)||(bool)type.GetField("friendly").GetValue(shot))continue;
            var obj=(GameObject)type.GetField("obj").GetValue(shot);var p=obj.transform.position;
            bool reflectable=(bool)type.GetField("reflectable").GetValue(shot);
            if(reflectable && p.z>0 && p.z<5 && Mathf.Abs(p.x-x)<2 && game.State.CooldownRemaining<=0)pulse=true;
            else if(p.z>0 && p.z<6 && Mathf.Abs(p.x-x)<1.5f)target=Mathf.Clamp(p.x+(x<p.x?-2f:2f),-6,6);
        }
        foreach(var drone in (IEnumerable)drones.GetValue(game))
        {
            var type=drone.GetType();if(!(bool)type.GetField("live").GetValue(drone))continue;
            var obj=(Transform)type.GetField("obj").GetValue(drone);var p=obj.position;
            if(p.z>0 && p.z<6 && Mathf.Abs(p.x-x)<1.8f)target=Mathf.Clamp(p.x+(x<p.x?-2.5f:2.5f),-6,6);
        }
        var keys=new List<Key>();if(target<x-.15f)keys.Add(Key.A);else if(target>x+.15f)keys.Add(Key.D);
        if(pulse && EditorApplication.timeSinceStartup-lastPulse>.2){keys.Add(Key.Space);lastPulse=EditorApplication.timeSinceStartup;}
        InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys.ToArray()));
    }
    static void Finish(string phase)
    {
        EditorApplication.update-=Tick;
        if(keyboard!=null && keyboard.added)InputSystem.RemoveDevice(keyboard);
        frames.Sort();float median=frames.Count>0?frames[frames.Count/2]:0;
        Directory.CreateDirectory("Screenshots");
        File.WriteAllText("Screenshots/flight-verification.txt",$"Phase={phase}\nElapsed={game?.Elapsed}\nHull={game?.State.Hull}\nScore={game?.State.Score}\nParries={game?.State.Parries}\nSamples={frames.Count}\nMedianEditorFrameMs={median}\n");
        Debug.Log("Flight verification: "+phase);
    }
}
