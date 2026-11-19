using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
namespace CyberDive
{
    public sealed class BridgeClient : MonoBehaviour
    {
        [Serializable] public class Pose { public bool valid,active; public long revision,seq; public string sessionId,gesture; public float confidence,ageMs; }
        [Serializable] public class Controller { public bool valid,left,right;public int direction;public float ageMs; }
        readonly PoseGate gate=new PoseGate();
        public bool JoystickMode=true;
        public bool DevicesEnabled {get;private set;}
        public bool PoseFresh => pose!=null && pose.valid && InputAge.Fresh(Time.realtimeSinceStartupAsDouble,poseAt,pose.ageMs,.5);
        public bool ControllerFresh => controller!=null && controller.valid && InputAge.Fresh(Time.realtimeSinceStartupAsDouble,controllerAt,controller.ageMs,.2);
        public string Gesture => PoseFresh?pose.gesture:"OFFLINE";
        public float Direction => !DevicesEnabled || !ControllerFresh?0:JoystickMode?controller.direction:controller.left==controller.right?0:controller.left?-1:1;
        Pose pose;Controller controller;double poseAt,controllerAt; bool pending;
        public bool AcceptSkills;
        public void SetDevicesEnabled(bool enabled)
        {
            StopAllCoroutines();DevicesEnabled=enabled;pose=null;controller=null;Disarm();
            if(enabled){StartCoroutine(PollPose());StartCoroutine(PollController());}
        }
        public bool ConsumeParry(){bool value=DevicesEnabled && pending && PoseFresh && AcceptSkills;pending=false;return value;}
        public void Disarm(){gate.Reset();pending=false;}
        void Update(){if(!PoseFresh || !AcceptSkills)Disarm();}
        IEnumerator PollPose()
        {
            while(true)
            {
                using(var req=UnityWebRequest.Get("http://127.0.0.1:8787/pose/latest"))
                {
                    double requestedAt=Time.realtimeSinceStartupAsDouble;
                    req.timeout=1;yield return req.SendWebRequest();
                    if(req.result==UnityWebRequest.Result.Success)
                    {
                        Pose p=null;try{p=JsonUtility.FromJson<Pose>(req.downloadHandler.text);}catch(ArgumentException){}
                        if(p!=null && p.valid && p.ageMs>=0 && p.ageMs<=500 && p.confidence>=0 && p.confidence<=1)
                        {
                            if(pose==null || pose.seq!=p.seq || pose.sessionId!=p.sessionId){poseAt=requestedAt;pose=p;}
                            if(AcceptSkills && p.confidence>=.7f){pending|=gate.Observe(p.sessionId,p.seq,p.gesture,p.active,PoseFresh);}
                            else Disarm();
                        }
                        else {pose=null;Disarm();}
                    }
                    else {pose=null;Disarm();}
                }
                yield return new WaitForSecondsRealtime(.075f);
            }
        }
        IEnumerator PollController()
        {
            while(true)
            {
                using(var req=UnityWebRequest.Get("http://127.0.0.1:8787/controller/latest"))
                {
                    double requestedAt=Time.realtimeSinceStartupAsDouble;
                    req.timeout=1;yield return req.SendWebRequest();
                    if(req.result==UnityWebRequest.Result.Success)
                    {
                        Controller p=null;try{p=JsonUtility.FromJson<Controller>(req.downloadHandler.text);}catch(ArgumentException){}
                        controller=p!=null && p.ageMs>=0 && p.ageMs<=200 && p.direction>=-1 && p.direction<=1?p:null;
                        controllerAt=requestedAt;
                    }
                    else controller=null;
                }
                yield return new WaitForSecondsRealtime(.05f);
            }
        }
    }
}
