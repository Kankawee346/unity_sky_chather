using System;
using UnityEngine;

// ตัวกลางคุมคะแนน / streak / level / เวลาเล่น ของทั้งเกม
// ใส่ไว้บน GameObject เปล่าตัวเดียวใน Scene (เช่น ชื่อ "GameManager")
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Session Info (โครงสร้างข้อมูลที่จะส่งไป Firebase/Google Sheet)")]
    public string userId = "guest";
    public int score = 0;
    public int level = 1;      // 1-3 ตามสเปค ความเร็วของสิ่งของที่ตก (ปรับได้จากหน้า Pause)
    public int complexity = 2; // 1=Easy, 2=Normal, 3=Hard ความซับซ้อนในการรับของ (ปรับได้จากหน้า Start Menu)
    public int playCount = 0;
    public float sessionTime = 0f;
    public const float RoundSeconds = 30f;
    public int itemsSeen { get; private set; }
    public int itemsCaught { get; private set; }
    private string roundId;

    [Header("Fuel")]
    public float maxFuel = 100f;
    public float fuel = 100f;
    [Tooltip("Fuel lost per second while playing (not while paused)")]
    public float drainPerSecond = 4f;

    private int currentStreak = 0;
    public int CurrentStreak { get { return currentStreak; } }
    private bool sessionRunning = false;
    public bool IsRunning { get { return sessionRunning; } }

    // ให้ UIManager หรือสคริปต์อื่น subscribe เพื่ออัปเดตหน้าจอ
    public event Action<int> OnScoreChanged;
    public event Action<string> OnStreakMessage;   // "ok!" / "ดี!" / "ดีมาก!" / "ดีที่สุด!"
    public event Action<int> OnStreakChanged;      // ส่งตัวเลข streak ปัจจุบันทุกครั้งที่เปลี่ยน (รับได้/พลาด)
    public event Action OnMiss;
    public event Action<float, float> OnFuelChanged;  // (current, max)
    public event Action OnHit;                        // plane hit an obstacle
    public event Action OnGameOver;                   // fuel reached 0
    public event Action OnSessionStarted;             // new round begins (Play / Restart)

    [Serializable]
    public class SessionData
    {
        public string user;
        public int score;
        public float time;
        public int level;
        public int playCount;
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Update()
    {
        if (!sessionRunning) return;

        sessionTime += Time.deltaTime;

        if (sessionTime >= RoundSeconds) GameOver();
    }

    // Harder complexity = faster drain
    float FuelDrainMultiplier()
    {
        switch (complexity)
        {
            case 1: return 0.8f;
            case 3: return 1.25f;
            default: return 1f;
        }
    }

    void SetFuel(float value)
    {
        fuel = Mathf.Clamp(value, 0f, maxFuel);
        OnFuelChanged?.Invoke(fuel, maxFuel);
    }

    public void AddFuel(float amount)
    {
        SetFuel(fuel + amount);
    }

    public void AddScore(int amount)
    {
        score += amount;
        OnScoreChanged?.Invoke(score);
    }

    // Plane hit an obstacle: lose fuel and break the combo
    public void RegisterHit(float fuelDamage)
    {
        currentStreak = 0;
        OnStreakChanged?.Invoke(currentStreak);
        OnHit?.Invoke();
        SetFuel(fuel - fuelDamage);
        // Damage is visible in the gauge, but the player finishes the 30-second round.
    }

    void GameOver()
    {
        if (!sessionRunning) return;
        EndSession();
        OnGameOver?.Invoke();
    }

    // เรียกตอนเริ่มรอบเล่นใหม่
    public void StartSession()
    {
        string savedName = PlayerPrefs.GetString("player_name", "").Trim();
        if (!string.IsNullOrEmpty(savedName)) userId = savedName;

        score = 0;
        level = Mathf.Clamp(complexity, 1, 3);
        itemsSeen = 0;
        itemsCaught = 0;
        roundId = Guid.NewGuid().ToString("N");
        currentStreak = 0;
        sessionTime = 0f;
        playCount++;
        sessionRunning = true;
        SetFuel(maxFuel);

        // ยิง event ทันที เพื่อให้ UI รีเซ็ตเป็น 0 ตั้งแต่วินาทีที่เริ่มเซสชันใหม่
        OnScoreChanged?.Invoke(score);
        OnStreakChanged?.Invoke(currentStreak);
        OnSessionStarted?.Invoke();
    }

    // เรียกจาก FallingObject เมื่อจับของได้ทันจังหวะ
    public void RegisterCatch(int scoreValue)
    {
        if (!sessionRunning) return;
        itemsCaught++;
        score += scoreValue;
        currentStreak++;
        OnScoreChanged?.Invoke(score);
        OnStreakChanged?.Invoke(currentStreak);

        string msg = GetStreakMessage(currentStreak);
        if (msg != null) OnStreakMessage?.Invoke(msg);
    }

    // เรียกจาก FallingObject เมื่อของตกพ้นจุดรับโดยไม่ถูกจับ
    public void RegisterMiss()
    {
        if (!sessionRunning) return;
        currentStreak = 0;
        OnStreakChanged?.Invoke(currentStreak);
        SetFuel(fuel - 8f);
        OnMiss?.Invoke();
        OnHit?.Invoke();
    }

    string GetStreakMessage(int streak)
    {
        if (streak == 5) return FriendLanguageUI.T("AMAZING!", "ยอดเยี่ยม!");
        if (streak == 4) return FriendLanguageUI.T("GREAT!", "เยี่ยมมาก!");
        if (streak == 3) return FriendLanguageUI.T("GOOD!", "ดีมาก!");
        if (streak == 2) return FriendLanguageUI.T("OK!", "ดี!");
        return null;
    }

    public void RegisterSpawn()
    {
        if (sessionRunning) itemsSeen++;
    }

    // เรียกตอนจบรอบ/จบเกม เพื่อเตรียมข้อมูลไปบันทึกลง Google Sheet หรือ Firebase
    public void EndSession()
    {
        if (!sessionRunning) return;   // safe to call twice
        sessionRunning = false;

        var data = new SessionData
        {
            user = userId,
            score = score,
            time = sessionTime,
            level = level,
            playCount = playCount
        };

        Leaderboard.SaveScore(data.user, data.score);

        if (sessionTime > 0.05f)
            StartCoroutine(RoundReporter.Save(new RoundReporter.Result {
                roundId = roundId, playerId = SafePlayerId(userId), score = score, level = level,
                durationMs = Mathf.RoundToInt(sessionTime * 1000f),
                itemsCaught = itemsCaught, itemsSeen = itemsSeen
            }));
    }

    private static string SafePlayerId(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "guest";
        var result = new System.Text.StringBuilder(40);
        foreach (char c in name.Trim())
        {
            if (result.Length == 40) break;
            var category = char.GetUnicodeCategory(c);
            bool thaiMark = category == System.Globalization.UnicodeCategory.NonSpacingMark ||
                category == System.Globalization.UnicodeCategory.SpacingCombiningMark;
            if (char.IsLetterOrDigit(c) || thaiMark || c == ' ' || c == '_' || c == '-') result.Append(c);
        }
        string value = result.ToString().Trim();
        return value.Length == 0 ? "guest" : value;
    }
}
