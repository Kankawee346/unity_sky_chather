using UnityEngine;
using TMPro;

// Shows the elapsed round time and the selected difficulty at the top of the HUD.
// Put it on any HUD object and assign a TMP text (it hides with the HUD, so it only shows while playing).
public class HudStatusUI : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI statusText;

    [Tooltip("Format: {0} = time, {1} = difficulty (e.g. \"TIME {0}   {1}\")")]
    public string format = "TIME {0}   {1}";

    void Update()
    {
        if (statusText == null) return;
        var gm = GameManager.Instance;
        if (gm == null) return;

        statusText.text = string.Format(format, FormatTime(gm.sessionTime), DifficultyLabel(gm.complexity));
    }

    // mm:ss (minutes keep growing past 99 instead of wrapping)
    public static string FormatTime(float seconds)
    {
        if (seconds < 0f) seconds = 0f;
        int total = Mathf.FloorToInt(seconds);
        return (total / 60).ToString("00") + ":" + (total % 60).ToString("00");
    }

    public static string DifficultyLabel(int complexity)
    {
        switch (complexity)
        {
            case 1: return "EASY";
            case 3: return "HARD";
            default: return "NORMAL";
        }
    }
}
