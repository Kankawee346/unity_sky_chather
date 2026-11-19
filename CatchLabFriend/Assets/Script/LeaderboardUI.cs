using TMPro;
using UnityEngine;

public class LeaderboardUI : MonoBehaviour
{
    [Header("Name Input")]
    public TMP_InputField nameInput;

    [Header("Ranking Display (top entries, one per line)")]
    public TMP_Text rankingText;

    [Header("Display")]
    public int maxLines = 8;

    void OnEnable()
    {
        if (nameInput != null)
        {
            string saved = PlayerPrefs.GetString("player_name", "");
            if (!string.IsNullOrEmpty(saved)) nameInput.text = saved;
            nameInput.onEndEdit.RemoveListener(OnNameChanged);
            nameInput.onEndEdit.AddListener(OnNameChanged);
            nameInput.onValueChanged.RemoveListener(OnNameChanged);
            nameInput.onValueChanged.AddListener(OnNameChanged);
        }
        Refresh();
    }

    void OnDisable()
    {
        if (nameInput != null)
        {
            nameInput.onEndEdit.RemoveListener(OnNameChanged);
            nameInput.onValueChanged.RemoveListener(OnNameChanged);
        }
    }

    void OnNameChanged(string value)
    {
        if (GameManager.Instance != null) GameManager.Instance.userId = string.IsNullOrWhiteSpace(value) ? "guest" : value.Trim();
        PlayerPrefs.SetString("player_name", value ?? "");
        PlayerPrefs.Save();
    }

    public void Refresh()
    {
        if (rankingText == null) return;
        var entries = Leaderboard.Load();
        if (entries.Count == 0)
        {
            rankingText.text = FriendLanguageUI.T("NO SCORES YET", "ยังไม่มีคะแนน");
            return;
        }

        var sb = new System.Text.StringBuilder();
        int n = Mathf.Min(entries.Count, maxLines);
        for (int i = 0; i < n; i++)
            sb.AppendLine((i + 1) + ". " + entries[i].name + " - " + entries[i].score);
        rankingText.text = sb.ToString().TrimEnd();
    }
}
