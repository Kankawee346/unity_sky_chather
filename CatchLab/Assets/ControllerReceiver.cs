using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

[RequireComponent(typeof(CatchGame))]
public sealed class ControllerReceiver : MonoBehaviour
{
    private IEnumerator Start()
    {
        var game = GetComponent<CatchGame>();
        string endpoint = BridgeEndpoint.Resolve("/controller/latest");
        if (endpoint == null)
            yield break;
        var retryDelay = new WaitForSecondsRealtime(0.1f);
        string revision = null;

        while (true)
        {
            string url = revision == null ? endpoint : endpoint + "?after=" + revision;
            using (var request = UnityWebRequest.Get(url))
            {
                request.timeout = 1;
                yield return request.SendWebRequest();
                if (request.result == UnityWebRequest.Result.Success && request.responseCode == 200)
                {
                    ApplySample(game, request.downloadHandler.text);
                    revision = request.GetResponseHeader("X-CatchLab-Revision");
                    if (!string.IsNullOrEmpty(revision))
                        continue;
                }
            }
            yield return retryDelay;
        }
    }

    private static void ApplySample(CatchGame game, string sample)
    {
        string[] fields = sample.Split(',');
        if (fields.Length != 3 ||
            (fields[0] != "0" && fields[0] != "1") ||
            (fields[1] != "0" && fields[1] != "1") ||
            (fields[2] != "-1" && fields[2] != "0" && fields[2] != "1"))
            return;

        int direction = fields[2] == "-1" ? -1 : fields[2] == "1" ? 1 : 0;
        game.ReceiveSensorSample(fields[0] == "1", fields[1] == "1", direction);
    }
}
