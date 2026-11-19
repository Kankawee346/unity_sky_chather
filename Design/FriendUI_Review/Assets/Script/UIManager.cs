using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

// แนบสคริปต์นี้ไว้บน GameObject ใต้ Canvas (เช่น ตั้งชื่อ "UIManager")
public class UIManager : MonoBehaviour
{
    [Header("ลาก Text (TMP) จาก Canvas มาใส่ตรงนี้")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI messageText;
    public TextMeshProUGUI comboText;

    [Header("Fuel gauge (legacy single fill) / handled by FuelBarUI if present")]
    public Image fuelFill;
    public FuelBarUI fuelBarUI;
    public Color fuelFullColor = new Color(0.3f, 0.85f, 0.3f);
    public Color fuelLowColor = new Color(0.9f, 0.2f, 0.2f);
    [Range(0f, 1f)] public float lowFuelThreshold = 0.25f;

    [Tooltip("ระยะเวลาที่ข้อความคำชมค้างอยู่บนจอ (วินาที)")]
    public float messageDuration = 1f;

    private Coroutine hideMessageRoutine;

    void Start()
    {
        // Start() รันหลัง Awake() ของทุกอ็อบเจกต์เสมอ จึงมั่นใจได้ว่า
        // GameManager.Instance ถูกตั้งค่าแล้วแน่นอนตอนมาถึงบรรทัดนี้
        GameManager.Instance.OnScoreChanged += UpdateScore;
        GameManager.Instance.OnStreakMessage += ShowMessage;
        GameManager.Instance.OnStreakChanged += UpdateCombo;
        GameManager.Instance.OnMiss += ClearMessage;
        GameManager.Instance.OnFuelChanged += UpdateFuel;
        GameManager.Instance.OnHit += ClearMessage;
        GameManager.Instance.OnGameOver += ShowGameOver;

        UpdateFuel(GameManager.Instance.fuel, GameManager.Instance.maxFuel);
        UpdateScore(GameManager.Instance.score);
        UpdateCombo(0);
        messageText.text = "";
    }

    void OnDestroy()
    {
        if (GameManager.Instance == null) return;
        GameManager.Instance.OnScoreChanged -= UpdateScore;
        GameManager.Instance.OnStreakMessage -= ShowMessage;
        GameManager.Instance.OnStreakChanged -= UpdateCombo;
        GameManager.Instance.OnMiss -= ClearMessage;
        GameManager.Instance.OnFuelChanged -= UpdateFuel;
        GameManager.Instance.OnHit -= ClearMessage;
        GameManager.Instance.OnGameOver -= ShowGameOver;
    }

    void UpdateFuel(float current, float max)
    {
        if (fuelBarUI != null) return; // animated/segmented bar handles its own display
        if (fuelFill == null) return;
        float t = max > 0f ? current / max : 0f;
        fuelFill.fillAmount = t;
        fuelFill.color = t <= lowFuelThreshold ? fuelLowColor : fuelFullColor;
    }

    void ShowGameOver()
    {
        ShowMessage("Out of fuel!");
    }

    void UpdateScore(int score)
    {
        scoreText.text = "Score: " + score;
    }

    // เรียกอัตโนมัติทุกครั้งที่ streak เปลี่ยน (รับได้ก็เพิ่ม พลาดก็รีเซ็ตเป็น 0)
    void UpdateCombo(int streak)
    {
        comboText.text = "Combo: " + streak;
    }

    // เรียกอัตโนมัติทุกครั้งที่ GameManager.RegisterCatch() ทำให้ streak ถึงเกณฑ์ (2,3,4,5+)
    void ShowMessage(string msg)
    {
        messageText.text = msg;

        if (hideMessageRoutine != null) StopCoroutine(hideMessageRoutine);
        hideMessageRoutine = StartCoroutine(HideMessageAfterDelay());
    }

    // เคลียร์ข้อความทันทีเมื่อพลาด (streak รีเซ็ต)
    void ClearMessage()
    {
        messageText.text = "";
    }

    IEnumerator HideMessageAfterDelay()
    {
        yield return new WaitForSeconds(messageDuration);
        messageText.text = "";
    }
}