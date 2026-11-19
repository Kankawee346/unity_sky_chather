using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// English keeps the friend's pixel font. Thai uses the bundled Sarabun font.
public sealed class FriendLanguageUI : MonoBehaviour
{
    public static bool Thai { get; private set; }
    public static string T(string english, string thai) { return Thai ? thai : english; }

    private readonly Dictionary<TextMeshProUGUI, string> originalCaptions = new Dictionary<TextMeshProUGUI, string>();
    private readonly Dictionary<TextMeshProUGUI, TMP_FontAsset> originalFonts = new Dictionary<TextMeshProUGUI, TMP_FontAsset>();
    private readonly Dictionary<TextMeshProUGUI, float> originalSizes = new Dictionary<TextMeshProUGUI, float>();
    private TMP_FontAsset thaiFont;
    private TextMeshProUGUI englishLabel;
    private TextMeshProUGUI thaiLabel;
    private TextMeshProUGUI titleLabel;
    private TMP_FontAsset titleEnglishFont;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        var game = FindAnyObjectByType<GameManager>();
        if (game != null && game.GetComponent<FriendLanguageUI>() == null)
            game.gameObject.AddComponent<FriendLanguageUI>();
    }

    private void Awake()
    {
        Thai = PlayerPrefs.GetInt("friend_thai", 0) == 1;
    }

    private void Start()
    {
        var font = Resources.Load<Font>("Fonts/Sarabun-Regular");
        if (font != null) thaiFont = TMP_FontAsset.CreateFontAsset(font);
        MakeButtons();
        var canvas = FindAnyObjectByType<Canvas>(FindObjectsInactive.Include);
        if (canvas != null)
            foreach (var label in canvas.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if (label == englishLabel || label == thaiLabel || label == titleLabel) continue;
                originalCaptions[label] = label.text;
                originalFonts[label] = label.font;
                originalSizes[label] = label.fontSize;
            }
        Apply();
    }

    private void MakeButtons()
    {
        var menu = FindAnyObjectByType<PauseMenuController>(FindObjectsInactive.Include);
        if (menu == null || menu.startMenuPanel == null) return;
        var root = new GameObject("LanguageSelector", typeof(RectTransform));
        root.transform.SetParent(menu.startMenuPanel.transform, false);
        var rect = root.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 0f);
        rect.pivot = Vector2.zero;
        rect.anchoredPosition = new Vector2(20f, 26f);
        rect.sizeDelta = new Vector2(108f, 46f);
        TextMeshProUGUI sample = menu.startMenuOptionTexts != null && menu.startMenuOptionTexts.Length > 0
            ? menu.startMenuOptionTexts[0] : null;
        englishLabel = MakeButton(root.transform, "EN", 27f, false, sample);
        thaiLabel = MakeButton(root.transform, "TH", 81f, true, sample);
        var title = new GameObject("GameTitle", typeof(RectTransform), typeof(TextMeshProUGUI));
        title.transform.SetParent(menu.startMenuPanel.transform, false);
        var titleRect = title.GetComponent<RectTransform>();
        titleRect.anchorMin = titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -24f);
        titleRect.sizeDelta = new Vector2(330f, 52f);
        titleLabel = title.GetComponent<TextMeshProUGUI>();
        if (sample != null) titleLabel.font = titleEnglishFont = sample.font;
        titleLabel.fontSize = 32f;
        titleLabel.alignment = TextAlignmentOptions.Center;
        titleLabel.color = new Color(1f, 0.88f, 0.31f);
        titleLabel.raycastTarget = false;
    }

    private TextMeshProUGUI MakeButton(Transform parent, string caption, float x, bool thai, TextMeshProUGUI sample)
    {
        var go = new GameObject(caption, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 0.5f);
        rect.anchoredPosition = new Vector2(x, 0f);
        rect.sizeDelta = new Vector2(50f, 46f);
        go.GetComponent<Image>().color = new Color(0.08f, 0.12f, 0.24f, 0.95f);
        var button = go.GetComponent<Button>();
        var colors = button.colors;
        colors.highlightedColor = new Color(1f, 0.85f, 0.45f);
        colors.pressedColor = new Color(1f, 0.55f, 0.2f);
        button.colors = colors;
        button.onClick.AddListener(() => SetLanguage(thai));
        var textObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(go.transform, false);
        var textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = textRect.offsetMax = Vector2.zero;
        var label = textObject.GetComponent<TextMeshProUGUI>();
        if (sample != null) label.font = sample.font;
        label.text = caption;
        label.fontSize = 20f;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        return label;
    }

    private void SetLanguage(bool thai)
    {
        Thai = thai;
        PlayerPrefs.SetInt("friend_thai", thai ? 1 : 0);
        PlayerPrefs.Save();
        Apply();
        var menu = FindAnyObjectByType<PauseMenuController>(FindObjectsInactive.Include);
        if (menu != null) menu.RefreshMapLabel();
        var leaderboard = FindAnyObjectByType<LeaderboardUI>(FindObjectsInactive.Include);
        if (leaderboard != null) leaderboard.Refresh();
        var ui = FindAnyObjectByType<UIManager>(FindObjectsInactive.Include);
        if (ui != null) ui.RefreshLanguage();
    }

    private void Apply()
    {
        if (englishLabel != null) englishLabel.color = Thai ? Color.gray : Color.yellow;
        if (thaiLabel != null) thaiLabel.color = Thai ? Color.yellow : Color.gray;
        if (titleLabel != null)
        {
            titleLabel.text = Thai ? "เกมรับของ" : "SKY CATCHER";
            titleLabel.fontSize = Thai ? 26f : 32f;
            if (Thai && thaiFont != null) titleLabel.font = thaiFont;
            else if (titleEnglishFont != null) titleLabel.font = titleEnglishFont;
        }
        foreach (var entry in originalCaptions)
        {
            if (entry.Key == null) continue;
            entry.Key.text = Thai ? Translate(entry.Value) : entry.Value;
            if (Thai && thaiFont != null) entry.Key.font = thaiFont;
            else if (originalFonts.TryGetValue(entry.Key, out var original)) entry.Key.font = original;
            if (originalSizes.TryGetValue(entry.Key, out var size))
                entry.Key.fontSize = Thai ? size * 0.62f : size;
        }
    }

    private static string Translate(string source)
    {
        string text = source.Trim().ToUpperInvariant();
        if (text.StartsWith("MAP:")) return "ฉาก: " + MapName(text.Substring(4).Trim());
        switch (text)
        {
            case "EASY": return "ง่าย";
            case "NORMAL": return "ปกติ";
            case "HARD": return "ยาก";
            case "PLAY": return "เล่น";
            case "PAUSE": return "หยุดชั่วคราว";
            case "CONTINUE": return "เล่นต่อ";
            case "RESTART": return "เริ่มใหม่";
            case "BACK TO MENU": return "กลับเมนู";
            case "RANKING": return "อันดับ";
            case "NO SCORES YET": return "ยังไม่มีคะแนน";
            case "PLAYER NAME": return "ชื่อผู้เล่น";
            case "ENTER NAME": return "ใส่ชื่อ";
            case "FUEL":
            case "HP": return "พลัง";
            case "GAME OVER":
            case "ROUND COMPLETE": return "จบรอบ";
            case "OUT OF FUEL":
            case "TIME UP": return "หมดเวลา";
            case "PRESS ANY BUTTON TO CONTINUE": return "กดปุ่มใดก็ได้";
            default: return source;
        }
    }

    public static string MapName(string english)
    {
        if (!Thai) return english;
        switch (english.ToUpperInvariant())
        {
            case "SKY": return "ท้องฟ้า";
            case "SPACE": return "อวกาศ";
            case "NIGHT SKY": return "ฟ้ายามคืน";
            default: return english;
        }
    }
}
