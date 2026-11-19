using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

// Independent of fast input polling. One in-flight request; retry latest state only.
public sealed class GameStatusSender : MonoBehaviour
{
    [System.Serializable] private class Status
    {
        public int score, seconds, level;
        public string mode, state;
    }
    private IEnumerator Start()
    {
        var game=GetComponent<CatchGame>();
        string previous=""; float sentAt=-10;
        while(true)
        {
            bool ready=!game.Playing && game.SaveState.Length==0;
            var status=new Status { score=ready?0:game.Score, seconds=ready?30:Mathf.CeilToInt(game.Remaining), level=game.Level+1,
                mode=game.Joystick?"JOY":"FSR", state=game.Playing?(game.Paused?"PAUSE":"PLAY"):game.SaveState.Length>0?"END":"READY" };
            string json=JsonUtility.ToJson(status);
            string url=BridgeEndpoint.Resolve("/game/status");
            if(url!=null && (json!=previous || Time.unscaledTime-sentAt>2))
            {
                using(var request=new UnityWebRequest(url,"POST"))
                {
                    request.uploadHandler=new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
                    request.downloadHandler=new DownloadHandlerBuffer();
                    request.SetRequestHeader("Content-Type","application/json"); request.timeout=2;
                    yield return request.SendWebRequest();
                    if(request.result==UnityWebRequest.Result.Success) { previous=json; sentAt=Time.unscaledTime; }
                }
            }
            yield return new WaitForSecondsRealtime(.2f);
        }
    }
}
