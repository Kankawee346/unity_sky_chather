using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

internal static class RoundReporter
{
    [Serializable]
    internal sealed class Result
    {
        public string roundId;
        public int score;
        public int level;
        public int durationMs;
        public int itemsCaught;
        public int itemsSeen;
    }

    public static IEnumerator Save(Result result, Action<bool> completed = null)
    {
        string endpoint = BridgeEndpoint.Resolve("/rounds");
        if (endpoint == null)
        {
            completed?.Invoke(false);
            yield break;
        }

        string json = JsonUtility.ToJson(result);
        for (int attempt = 0; attempt < 2; attempt++)
        {
            using (var request = UnityWebRequest.Post(endpoint, json, "application/json"))
            {
                request.timeout = 10;
                yield return request.SendWebRequest();
                if (request.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log($"CatchLab round saved. {request.downloadHandler.text}");
                    completed?.Invoke(true);
                    yield break;
                }
                if (request.responseCode == 503)
                {
                    Debug.Log("CatchLab round not saved. Configure Firebase on the bridge first.");
                    completed?.Invoke(false);
                    yield break;
                }
                if (attempt == 1 || (request.responseCode > 0 && request.responseCode < 500))
                {
                    Debug.LogWarning($"CatchLab round save failed ({request.responseCode}).");
                    completed?.Invoke(false);
                    yield break;
                }
            }

            yield return new WaitForSecondsRealtime(0.5f);
        }
    }
}
