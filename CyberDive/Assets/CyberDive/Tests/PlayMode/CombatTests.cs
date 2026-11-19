using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.EventSystems;
namespace CyberDive.Tests
{
    public class CombatTests
    {
        [UnityTest] public IEnumerator HudShowsHullAndParryWhileTelemetryStartsHidden()
        {
            var type=GameType();var root=new GameObject("HudRegression");
            try
            {
                var game=(MonoBehaviour)root.AddComponent(type);yield return null;game.enabled=false;
                var hud=root.transform.Find("TerminalHUD/FlightHUD");Assert.That(hud,Is.Not.Null,"Flight HUD must have a clear hierarchy");
                Assert.That(hud.Find("Telemetry").gameObject.activeSelf,Is.False,"Debug readings should not cover the playfield by default");
                type.GetMethod("BeginRun").Invoke(game,null);var state=(RunState)type.GetProperty("State").GetValue(game);
                state.Damage(AudioSettings.dspTime);state.TryParry(AudioSettings.dspTime);yield return null;
                var full=hud.Find("Survival/Hull1").GetComponent<UnityEngine.UI.Image>();
                var empty=hud.Find("Survival/Hull3").GetComponent<UnityEngine.UI.Image>();
                Assert.That(full.color.a,Is.GreaterThan(empty.color.a));
                var ring=hud.Find("Parry/Charge").GetComponent("HudRing");Assert.That(ring,Is.Not.Null);
                Assert.That((float)ring.GetType().GetProperty("Fill").GetValue(ring),Is.LessThan(.1f));
            }
            finally {UnityEngine.Object.Destroy(root);}
        }
        [UnityTest] public IEnumerator ParryEnergyCueRespectsLowFxAndClearsOnPause()
        {
            var type=GameType();var root=new GameObject("ParryFxRegression");Keyboard keyboard=null;
            try
            {
                var game=(MonoBehaviour)root.AddComponent(type);yield return null;keyboard=InputSystem.AddDevice<Keyboard>();
                type.GetField("ReducedEffects").SetValue(game,true);type.GetMethod("BeginRun").Invoke(game,null);
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Space));yield return null;yield return null;
                var state=(RunState)type.GetProperty("State").GetValue(game);Assert.That(state.ShieldActive,Is.True);
                var fx=root.GetComponent("CombatFeedback");var count=fx.GetType().GetProperty("ActiveCount");Assert.That((int)count.GetValue(fx),Is.Zero);
                InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;
                type.GetField("ReducedEffects").SetValue(game,false);type.GetMethod("BeginRun").Invoke(game,null);
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Space));yield return null;yield return null;
                Assert.That((int)count.GetValue(fx),Is.GreaterThan(0),"Full FX must show a local energy burst when the shield opens");
                type.GetMethod("TogglePause").Invoke(game,null);yield return null;Assert.That((int)count.GetValue(fx),Is.Zero);
            }
            finally {if(keyboard!=null && keyboard.added)InputSystem.RemoveDevice(keyboard);UnityEngine.Object.Destroy(root);}
        }
        [UnityTest] public IEnumerator EnterDoesNotResubmitSelectedAudioOption()
        {
            string[] keys={"cyber-option-difficulty","cyber-option-muted","cyber-option-lowfx","cyber-option-shake","cyber-option-flash"};
            var had=new bool[keys.Length];var saved=new int[keys.Length];
            for(int i=0;i<keys.Length;i++){had[i]=PlayerPrefs.HasKey(keys[i]);saved[i]=PlayerPrefs.GetInt(keys[i],0);}
            var type=GameType();var root=new GameObject("EnterRegression");Keyboard keyboard=null;
            try
            {
                var game=(MonoBehaviour)root.AddComponent(type);yield return null;game.enabled=false;
                var audio=root.transform.Find("TerminalHUD/MissionPanel/Audio").gameObject;
                EventSystem.current.SetSelectedGameObject(audio);
                ExecuteEvents.Execute(audio,new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left},ExecuteEvents.pointerClickHandler);
                bool muted=(bool)type.GetProperty("Muted").GetValue(game);
                keyboard=InputSystem.AddDevice<Keyboard>();
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Enter));
                yield return null;yield return null;
                Assert.That((bool)type.GetProperty("Muted").GetValue(game),Is.EqualTo(muted),"Enter belongs to the game and must not click the selected menu option again");
            }
            finally
            {
                if(keyboard!=null && keyboard.added)InputSystem.RemoveDevice(keyboard);
                for(int i=0;i<keys.Length;i++){if(had[i])PlayerPrefs.SetInt(keys[i],saved[i]);else PlayerPrefs.DeleteKey(keys[i]);}PlayerPrefs.Save();
                UnityEngine.Object.Destroy(root);
            }
        }
        [UnityTest] public IEnumerator DifficultyLocksDuringRunAndOptionsSurviveReload()
        {
            string[] keys={"cyber-option-difficulty","cyber-option-muted","cyber-option-lowfx","cyber-option-shake","cyber-option-flash"};
            var had=new bool[keys.Length];var saved=new int[keys.Length];
            for(int i=0;i<keys.Length;i++){had[i]=PlayerPrefs.HasKey(keys[i]);saved[i]=PlayerPrefs.GetInt(keys[i],0);}
            var type=GameType();var root=new GameObject("OptionsRegression");var second=new GameObject("ReloadOptions");
            try
            {
                var game=(MonoBehaviour)root.AddComponent(type);yield return null;game.enabled=false;
                var select=type.GetMethod("TrySelectDifficulty");Assert.That(select,Is.Not.Null);
                Assert.That((bool)select.Invoke(game,new object[]{2}),Is.True);
                type.GetMethod("BeginRun").Invoke(game,null);
                Assert.That((int)type.GetProperty("RunDifficulty").GetValue(game),Is.EqualTo(2));
                var flags=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
                var spawn=type.GetMethod("SpawnShot",flags);
                spawn.Invoke(game,new object[]{new Vector3(0,0,20),Vector3.back*10,false,true});
                spawn.Invoke(game,new object[]{Vector3.zero,Vector3.forward*38,true,false});
                var shots=(System.Collections.IList)type.GetField("shots",flags).GetValue(game);
                Assert.That(((Vector3)shots[0].GetType().GetField("velocity").GetValue(shots[0])).z,Is.EqualTo(-12).Within(.001f));
                Assert.That(((Vector3)shots[1].GetType().GetField("velocity").GetValue(shots[1])).z,Is.EqualTo(38).Within(.001f));
                Assert.That((bool)select.Invoke(game,new object[]{0}),Is.False);
                type.GetMethod("TogglePause").Invoke(game,null);Assert.That((bool)select.Invoke(game,new object[]{0}),Is.False);
                type.GetMethod("TogglePause").Invoke(game,null);((RunState)type.GetProperty("State").GetValue(game)).Fail();
                Assert.That((bool)select.Invoke(game,new object[]{0}),Is.True);
                type.GetField("ReducedEffects").SetValue(game,true);type.GetField("Shake").SetValue(game,false);type.GetField("FlashEffects").SetValue(game,true);
                type.GetMethod("ToggleMute").Invoke(game,null);bool muted=(bool)type.GetProperty("Muted").GetValue(game);
                type.GetMethod("SaveOptions").Invoke(game,null);
                var reload=(MonoBehaviour)second.AddComponent(type);reload.enabled=false;
                Assert.That((int)type.GetProperty("SelectedDifficulty").GetValue(reload),Is.EqualTo(0));
                Assert.That((bool)type.GetProperty("Muted").GetValue(reload),Is.EqualTo(muted));
                Assert.That((bool)type.GetField("ReducedEffects").GetValue(reload),Is.True);
                Assert.That((bool)type.GetField("Shake").GetValue(reload),Is.False);
                Assert.That((bool)type.GetField("FlashEffects").GetValue(reload),Is.True);
            }
            finally {for(int i=0;i<keys.Length;i++){if(had[i])PlayerPrefs.SetInt(keys[i],saved[i]);else PlayerPrefs.DeleteKey(keys[i]);}PlayerPrefs.Save();UnityEngine.Object.Destroy(root);UnityEngine.Object.Destroy(second);}
        }
        [UnityTest] public IEnumerator FeedbackIsBoundedExpiresAndClearsWithoutChangingRun()
        {
            Type type=null;foreach(var a in AppDomain.CurrentDomain.GetAssemblies()){type=a.GetType("CyberDive.CombatFeedback");if(type!=null)break;}
            Assert.That(type,Is.Not.Null);var root=new GameObject("FeedbackRegression");var fx=root.AddComponent(type);
            var emit=type.GetMethod("Emit");var active=type.GetProperty("ActiveCount");
            emit.Invoke(fx,new object[]{Vector3.zero,true,false});Assert.That((int)active.GetValue(fx),Is.EqualTo(1));
            for(int i=0;i<100;i++)emit.Invoke(fx,new object[]{Vector3.zero,false,true});
            Assert.That((int)active.GetValue(fx),Is.LessThanOrEqualTo(16));
            type.GetMethod("Tick").Invoke(fx,new object[]{2f});Assert.That((int)active.GetValue(fx),Is.Zero);
            emit.Invoke(fx,new object[]{Vector3.zero,true,false});type.GetMethod("Clear").Invoke(fx,null);Assert.That((int)active.GetValue(fx),Is.Zero);
            UnityEngine.Object.Destroy(root);yield return null;
        }
        [UnityTest] public IEnumerator ImmediateRestartPreservesFinishedBestScore()
        {
            bool existed=PlayerPrefs.HasKey("cyber-dive-best");int saved=PlayerPrefs.GetInt("cyber-dive-best",0);
            var type=GameType();var root=new GameObject("BestScoreRegression");
            try
            {
                var game=(MonoBehaviour)root.AddComponent(type);yield return null;game.enabled=false;
                type.GetMethod("BeginRun").Invoke(game,null);
                var state=(RunState)type.GetProperty("State").GetValue(game);
                state.AwardKill(saved+127);state.DefeatBoss();type.GetMethod("BeginRun").Invoke(game,null);
                Assert.That((int)type.GetProperty("BestScore").GetValue(game),Is.EqualTo(saved+127));
            }
            finally {if(existed)PlayerPrefs.SetInt("cyber-dive-best",saved);else PlayerPrefs.DeleteKey("cyber-dive-best");PlayerPrefs.Save();UnityEngine.Object.Destroy(root);}
        }
        [UnityTest] public IEnumerator DeadlineTakesPriorityOverLethalShot()
        {
            var type=GameType();var root=new GameObject("DeadlineRegression");var game=(MonoBehaviour)root.AddComponent(type);
            yield return null;game.enabled=false;type.GetMethod("BeginRun").Invoke(game,null);
            var flags=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
            type.GetField("origin",flags).SetValue(game,AudioSettings.dspTime-120.1);
            type.GetField("<BossHealth>k__BackingField",flags).SetValue(game,1);
            type.GetMethod("SpawnShot",flags).Invoke(game,new object[]{new Vector3(0,0,26),Vector3.forward*10000,true,false});
            type.GetMethod("Update",flags).Invoke(game,null);
            Assert.That(((RunState)type.GetProperty("State").GetValue(game)).Phase,Is.EqualTo(RunPhase.Lost));
            Assert.That((string)type.GetProperty("Feedback").GetValue(game),Is.EqualTo("MISSION TIMEOUT"));
            UnityEngine.Object.Destroy(root);yield return null;
        }
        [UnityTest] public IEnumerator KeyboardOnlyIgnoresExternalSamplesAndDisableClearsThem()
        {
            Type type=null;foreach(var a in AppDomain.CurrentDomain.GetAssemblies()){type=a.GetType("CyberDive.BridgeClient");if(type!=null)break;}
            Assert.That(type,Is.Not.Null);var root=new GameObject("KeyboardOnly");var receiver=(MonoBehaviour)root.AddComponent(type);receiver.enabled=false;
            var flags=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
            Assert.That((bool)type.GetProperty("DevicesEnabled").GetValue(receiver),Is.False);
            var dataType=type.GetNestedType("Controller");var data=Activator.CreateInstance(dataType);
            dataType.GetField("valid").SetValue(data,true);dataType.GetField("direction").SetValue(data,1);
            type.GetField("controller",flags).SetValue(receiver,data);type.GetField("controllerAt",flags).SetValue(receiver,Time.realtimeSinceStartupAsDouble);
            Assert.That((float)type.GetProperty("Direction").GetValue(receiver),Is.Zero);
            type.GetMethod("SetDevicesEnabled").Invoke(receiver,new object[]{false});
            Assert.That((bool)type.GetProperty("ControllerFresh").GetValue(receiver),Is.False);
            Assert.That((bool)type.GetMethod("ConsumeParry").Invoke(receiver,null),Is.False);
            UnityEngine.Object.Destroy(root);yield return null;
        }
        [UnityTest] public IEnumerator ShieldReflectsMarkedBulletsButNotObstacles()
        {
            var type=GameType();Assert.That(type,Is.Not.Null);
            var root=new GameObject("CollisionTest");var game=(MonoBehaviour)root.AddComponent(type);
            yield return null;game.enabled=false;type.GetMethod("BeginRun").Invoke(game,null);
            var state=(RunState)type.GetProperty("State").GetValue(game);
            var flags=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
            var spawn=type.GetMethod("SpawnShot",flags);var tick=type.GetMethod("UpdateShots",flags);
            state.TryParry(AudioSettings.dspTime);
            spawn.Invoke(game,new object[]{new Vector3(0,0,.001f),Vector3.back*10000,false,true});
            yield return null;tick.Invoke(game,null);
            Assert.That(state.Parries,Is.EqualTo(1));Assert.That(state.Hull,Is.EqualTo(3));
            spawn.Invoke(game,new object[]{new Vector3(0,0,.001f),Vector3.back*10000,false,false});
            tick.Invoke(game,null);Assert.That(state.Hull,Is.EqualTo(2));
            state.Pause(true,AudioSettings.dspTime);
            Assert.That(state.Damage(AudioSettings.dspTime+10),Is.False);
            UnityEngine.Object.Destroy(root);yield return null;
        }
        [UnityTest] public IEnumerator ExpiredPendingPoseCannotFireBeforeReceiverUpdate()
        {
            Type type=null;
            foreach(var a in AppDomain.CurrentDomain.GetAssemblies()){type=a.GetType("CyberDive.BridgeClient");if(type!=null)break;}
            Assert.That(type,Is.Not.Null);
            var root=new GameObject("PendingPoseTest");var receiver=(MonoBehaviour)root.AddComponent(type);receiver.enabled=false;
            type.GetMethod("SetDevicesEnabled").Invoke(receiver,new object[]{true});receiver.StopAllCoroutines();
            var flags=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
            var poseType=type.GetNestedType("Pose");var pose=Activator.CreateInstance(poseType);
            poseType.GetField("valid").SetValue(pose,true);poseType.GetField("ageMs").SetValue(pose,0f);
            type.GetField("pose",flags).SetValue(receiver,pose);
            type.GetField("poseAt",flags).SetValue(receiver,Time.realtimeSinceStartupAsDouble-1);
            type.GetField("pending",flags).SetValue(receiver,true);type.GetField("AcceptSkills").SetValue(receiver,true);
            Assert.That((bool)type.GetMethod("ConsumeParry").Invoke(receiver,null),Is.False);
            UnityEngine.Object.Destroy(root);yield return null;
        }
        static Type GameType()
        {
            foreach(var a in AppDomain.CurrentDomain.GetAssemblies())
            { var t=a.GetType("CyberDive.DiveGame"); if(t!=null)return t; }
            return null;
        }
        [UnityTest] public IEnumerator CombatAdvancesAndRestartClearsRun()
        {
            var t=GameType();Assert.That(t,Is.Not.Null,"Playable game component must exist");
            var root=new GameObject("TestRun");var game=root.AddComponent(t);
            yield return null;
            t.GetMethod("BeginRun").Invoke(game,null);
            var state=(RunState)t.GetProperty("State").GetValue(game);
            Assert.That(state.Phase,Is.EqualTo(RunPhase.Playing));
            yield return new WaitForSeconds(.5f);
            Assert.That((float)t.GetProperty("Elapsed").GetValue(game),Is.GreaterThan(.2f));
            Assert.That((int)t.GetProperty("ActiveShots").GetValue(game),Is.GreaterThan(0));
            double now=AudioSettings.dspTime;
            state.Damage(now+1);state.Damage(now+3);state.Damage(now+5);
            Assert.That(state.Phase,Is.EqualTo(RunPhase.Lost));
            t.GetMethod("BeginRun").Invoke(game,null);
            Assert.That(state.Hull,Is.EqualTo(3));Assert.That(state.Score,Is.Zero);
            UnityEngine.Object.Destroy(root);yield return null;
        }
    }
}
