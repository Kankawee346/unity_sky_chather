using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

// The friend scene stays independent from the original CatchLab scene.
public sealed class FriendControllerReceiver : MonoBehaviour
{
    public static int Direction { get; private set; }
    public static bool JoystickMode { get; private set; }
    private static float lastSampleAt;
    private TextMeshProUGUI fsrLabel;
    private TextMeshProUGUI joyLabel;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        var game = FindAnyObjectByType<GameManager>();
        if (game != null && game.GetComponent<FriendControllerReceiver>() == null)
            game.gameObject.AddComponent<FriendControllerReceiver>();
    }

    private void Awake()
    {
        JoystickMode = PlayerPrefs.GetInt("friend_joystick_mode", 0) == 1;
        Direction = 0;
    }

    private void Start()
    {
        MakeModeButtons();
        StartCoroutine(PollController());
        StartCoroutine(SendGameStatus());
    }

    private void Update()
    {
        if (Time.realtimeSinceStartup - lastSampleAt > 0.35f) Direction = 0;
    }

    private IEnumerator PollController()
    {
        string endpoint = BridgeEndpoint.Resolve("/controller/latest");
        if (endpoint == null) yield break;
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
                    string[] fields = request.downloadHandler.text.Trim().Split(',');
                    if (fields.Length == 3 &&
                        (fields[0] == "0" || fields[0] == "1") &&
                        (fields[1] == "0" || fields[1] == "1") &&
                        (fields[2] == "-1" || fields[2] == "0" || fields[2] == "1"))
                    {
                        bool left = fields[0] == "1";
                        bool right = fields[1] == "1";
                        Direction = JoystickMode ? int.Parse(fields[2]) : left == right ? 0 : left ? -1 : 1;
                        lastSampleAt = Time.realtimeSinceStartup;
                    }
                    revision = request.GetResponseHeader("X-CatchLab-Revision");
                    if (!string.IsNullOrEmpty(revision)) continue;
                }
            }
            yield return new WaitForSecondsRealtime(0.1f);
        }
    }

    [System.Serializable]
    private sealed class GameStatus
    {
        public int score, seconds, level;
        public string mode, state;
    }

    private IEnumerator SendGameStatus()
    {
        string endpoint = BridgeEndpoint.Resolve("/game/status");
        if (endpoint == null) yield break;
        string previous = "";
        float sentAt = -10f;
        while (true)
        {
            var game = GameManager.Instance;
            if (game != null)
            {
                string json = JsonUtility.ToJson(new GameStatus {
                    score = game.score,
                    seconds = game.IsRunning ? Mathf.Max(0, Mathf.CeilToInt(GameManager.RoundSeconds - game.sessionTime)) : game.sessionTime > 0f ? 0 : 30,
                    level = Mathf.Clamp(game.complexity, 1, 3),
                    mode = JoystickMode ? "JOY" : "FSR",
                    state = game.IsRunning ? (Time.timeScale == 0f ? "PAUSE" : "PLAY") : game.sessionTime > 0f ? "END" : "READY"
                });
                if (json != previous || Time.realtimeSinceStartup - sentAt > 2f)
                {
                    using (var request = new UnityWebRequest(endpoint, "POST"))
                    {
                        request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
                        request.downloadHandler = new DownloadHandlerBuffer();
                        request.SetRequestHeader("Content-Type", "application/json");
                        request.timeout = 2;
                        yield return request.SendWebRequest();
                        if (request.result == UnityWebRequest.Result.Success)
                        {
                            previous = json;
                            sentAt = Time.realtimeSinceStartup;
                        }
                    }
                }
            }
            yield return new WaitForSecondsRealtime(0.2f);
        }
    }

    private void SetMode(bool joystick)
    {
        JoystickMode = joystick;
        Direction = 0;
        PlayerPrefs.SetInt("friend_joystick_mode", joystick ? 1 : 0);
        PlayerPrefs.Save();
        RefreshLabels();
    }

    private void RefreshLabels()
    {
        if (fsrLabel != null) fsrLabel.color = JoystickMode ? new Color(0.65f, 0.7f, 0.8f) : Color.yellow;
        if (joyLabel != null) joyLabel.color = JoystickMode ? Color.yellow : new Color(0.65f, 0.7f, 0.8f);
    }

    private void MakeModeButtons()
    {
        var menu = FindAnyObjectByType<PauseMenuController>(FindObjectsInactive.Include);
        if (menu == null || menu.startMenuPanel == null) return;
        var root = new GameObject("SensorModeSelector", typeof(RectTransform));
        root.transform.SetParent(menu.startMenuPanel.transform, false);
        var rect = root.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, 26f);
        rect.sizeDelta = new Vector2(300f, 54f);

        TextMeshProUGUI sample = menu.startMenuOptionTexts != null && menu.startMenuOptionTexts.Length > 0
            ? menu.startMenuOptionTexts[0] : null;
        fsrLabel = MakeButton(root.transform, "FSR 2", -76f, false, sample);
        joyLabel = MakeButton(root.transform, "JOYSTICK", 76f, true, sample);
        RefreshLabels();
    }

    private TextMeshProUGUI MakeButton(Transform parent, string caption, float x, bool joystick, TextMeshProUGUI sample)
    {
        var go = new GameObject(caption, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(x, 0f);
        rect.sizeDelta = new Vector2(144f, 46f);
        go.GetComponent<Image>().color = new Color(0.08f, 0.12f, 0.24f, 0.95f);
        var button = go.GetComponent<Button>();
        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 0.85f, 0.45f);
        colors.pressedColor = new Color(1f, 0.55f, 0.2f);
        button.colors = colors;
        button.onClick.AddListener(() => SetMode(joystick));

        var labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(go.transform, false);
        var labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
        var label = labelObject.GetComponent<TextMeshProUGUI>();
        if (sample != null) label.font = sample.font;
        label.text = caption;
        label.fontSize = 20f;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        return label;
    }
}
