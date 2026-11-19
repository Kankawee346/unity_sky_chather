using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

// แนบไว้บน GameObject เดียว ควบคุมทั้ง Start Menu และ Pause Menu ด้วยปุ่มซ้าย-ขวาเพียง 2 ปุ่ม
// (ชื่อคลาสยังคงเดิมเพื่อไม่ให้การเชื่อมต่อใน Inspector ที่ทำไว้ก่อนหน้าหลุด)
public class PauseMenuController : MonoBehaviour
{
    [Header("ปุ่มที่ใช้ (ตอนนี้แทนด้วยคีย์บอร์ด ภายหลังสลับเป็นค่าจาก ESP32 ได้)")]
    public KeyCode leftKey = KeyCode.A;
    public KeyCode rightKey = KeyCode.D;

    [Tooltip("ต้องกดสองปุ่มพร้อมกันค้างไว้กี่วินาที ถึงจะนับว่าตั้งใจกดจริงๆ (กันกดพลาด)")]
    public float holdThreshold = 0.3f;

    [Tooltip("กดปุ่มเดี่ยวค้างไว้กี่วินาที ถึงจะเลื่อน cursor (ควรสั้นกว่า Hold Threshold ด้านบน)")]
    public float moveHoldThreshold = 0.15f;

    [Tooltip("Game Over: ignore input for this long before 'press any button to continue' activates (prevents instant skip)")]
    public float gameOverInputDelay = 0.5f;

    [Header("เชื่อมกับ SpawnManager / MapManager")]
    public SpawnManager spawnManager;
    public MapManager mapManager;

    [Header("Start Menu (เรียง Element: Easy, Normal, Hard, Map, Play)")]
    public GameObject startMenuPanel;
    public TextMeshProUGUI[] startMenuOptionTexts;

    [Header("Pause Menu (เรียง Element: Continue, Restart, Back to Start)")]
    public GameObject pausePanel;
    public TextMeshProUGUI[] optionTexts;

    [Header("Game Over (แสดงเมื่อเชื้อเพลิงหมด)")]
    public GameObject gameOverPanel;

    [Header("Gameplay UI (score, combo, message) - แสดงเฉพาะตอนเล่นเกม")]
    public GameObject hudRoot;

    [Header("Sounds")]
    [Tooltip("Played when any menu button/option is confirmed (start menu, pause menu, game over).")]
    public AudioClip buttonClickSound;
    [Range(0f, 1f)] public float buttonClickVolume = 1f;

    [Header("สี Cursor")]
    public Color normalColor = Color.white;
    public Color selectedColor = Color.yellow;

    private enum MenuState { StartMenu, Playing, PauseMenu, GameOver }
    private MenuState state = MenuState.StartMenu;

    private float bothHeldTimer = 0f;
    private bool chordTriggered = false;
    private float gameOverInputTimer = 0f;
    private float leftHoldTimer = 0f;
    private float rightHoldTimer = 0f;
    private bool leftMoveTriggered = false;
    private bool rightMoveTriggered = false;

    private int startCursorIndex = 0;
    private int pauseCursorIndex = 0;
    private AudioSource audioSource;

    void Start()
    {
        state = MenuState.StartMenu;
        if (mapManager == null) mapManager = FindAnyObjectByType<MapManager>(FindObjectsInactive.Include);

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f; // 2D menu sound

        if (pausePanel != null) pausePanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (startMenuPanel != null) startMenuPanel.SetActive(true);
        SetHudVisible(false);

        startCursorIndex = Mathf.Clamp(GameManager.Instance.complexity - 1, 0, MaxDifficultyIndex());
        pauseCursorIndex = 0;

        RefreshMapLabel();
        RefreshHighlight(startMenuOptionTexts, startCursorIndex);
        RefreshHighlight(optionTexts, pauseCursorIndex);

        // When the fuel runs out, show the Game Over screen
        GameManager.Instance.OnGameOver += HandleGameOver;
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null) GameManager.Instance.OnGameOver -= HandleGameOver;
    }

    void HandleGameOver()
    {
        Time.timeScale = 1f;
        if (pausePanel != null) pausePanel.SetActive(false);
        if (startMenuPanel != null) startMenuPanel.SetActive(false);
        if (gameOverPanel == null && spawnManager != null) gameOverPanel = spawnManager.gameOverPanel;
        if (gameOverPanel != null) gameOverPanel.SetActive(true);
        state = MenuState.GameOver;
        gameOverInputTimer = 0f;
        SetHudVisible(false);
    }

    void Update()
    {
        // Esc opens the pause menu during gameplay and closes it while paused
        var keyboard = Keyboard.current;
        var mouse = Mouse.current;
        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
        {
            if (state == MenuState.Playing) { EnterPause(); return; }
            if (state == MenuState.PauseMenu) { ResumeGame(); return; }
        }

        // Game Over: press any key / mouse button to continue (after a short input delay)
        if (state == MenuState.GameOver)
        {
            gameOverInputTimer += Time.unscaledDeltaTime;
            bool anyPressed = (keyboard != null && keyboard.anyKey.wasPressedThisFrame)
                || (mouse != null && (mouse.leftButton.wasPressedThisFrame
                    || mouse.rightButton.wasPressedThisFrame || mouse.middleButton.wasPressedThisFrame));
            if (gameOverInputTimer >= gameOverInputDelay && anyPressed)
            {
                ContinueFromGameOver();
                return;
            }
        }

        bool leftHeld = keyboard != null && (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed);
        bool rightHeld = keyboard != null && (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed);
        bool bothHeld = leftHeld && rightHeld;

        if (bothHeld)
        {
            // ใช้ unscaledDeltaTime เพราะตอน Pause อยู่ Time.deltaTime จะเป็น 0
            bothHeldTimer += Time.unscaledDeltaTime;

            if (bothHeldTimer >= holdThreshold && !chordTriggered)
            {
                chordTriggered = true;
                OnBothPressed();
            }

            leftHoldTimer = 0f;
            rightHoldTimer = 0f;
            leftMoveTriggered = false;
            rightMoveTriggered = false;
        }
        else
        {
            bothHeldTimer = 0f;
            chordTriggered = false;

            if (state == MenuState.StartMenu || state == MenuState.PauseMenu)
            {
                UpdateSoloHold(leftHeld, ref leftHoldTimer, ref leftMoveTriggered, -1);
                UpdateSoloHold(rightHeld, ref rightHoldTimer, ref rightMoveTriggered, 1);
            }
        }
    }

    // จัดการปุ่มเดี่ยวๆ ที่กดค้างไว้ (ไม่มีปุ่มอีกข้างมาร่วมด้วย) สำหรับเลื่อน cursor
    void UpdateSoloHold(bool keyHeld, ref float timer, ref bool triggered, int direction)
    {
        if (keyHeld)
        {
            timer += Time.unscaledDeltaTime;
            if (timer >= moveHoldThreshold && !triggered)
            {
                triggered = true;
                MoveCursor(direction);
            }
        }
        else
        {
            timer = 0f;
            triggered = false;
        }
    }

    void MoveCursor(int direction)
    {
        if (state == MenuState.StartMenu)
        {
            startCursorIndex = Wrap(startCursorIndex + direction, startMenuOptionTexts.Length);
            RefreshHighlight(startMenuOptionTexts, startCursorIndex);
        }
        else if (state == MenuState.PauseMenu)
        {
            pauseCursorIndex = Wrap(pauseCursorIndex + direction, optionTexts.Length);
            RefreshHighlight(optionTexts, pauseCursorIndex);
        }
    }

    // วนกลับไปต้น/ท้ายลิสต์อัตโนมัติ (เลื่อนเกินขวาสุด = วนไปซ้ายสุด และในทางกลับกัน)
    int Wrap(int index, int length)
    {
        return (index + length) % length;
    }

    void RefreshHighlight(TextMeshProUGUI[] texts, int selectedIndex)
    {
        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i] == null) continue;
            texts[i].color = (i == selectedIndex) ? selectedColor : normalColor;
        }
    }

    void SetHudVisible(bool visible)
    {
        if (hudRoot != null) hudRoot.SetActive(visible);
    }

    // Shared UI click sound for every menu button/option.
    void PlayClickSound()
    {
        if (buttonClickSound == null) return;
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
        }
        audioSource.PlayOneShot(buttonClickSound, buttonClickVolume);
    }

    void OnBothPressed()
    {
        if (state == MenuState.StartMenu) ConfirmStartMenu();
        else if (state == MenuState.Playing) return;
        else if (state == MenuState.PauseMenu) ConfirmPauseMenu();
        else if (state == MenuState.GameOver) ContinueFromGameOver();
    }

    public void ContinueFromGameOver()
    {
        if (state != MenuState.GameOver) return;
        PlayClickSound();
        BackToStartMenu();
    }

    // Mouse: select/click options in the Start Menu (Easy, Normal, Hard, Play)
    public void ClickStartOption(int index)
    {
        if (state != MenuState.StartMenu) return;
        if (index < 0 || index >= startMenuOptionTexts.Length) return;
        startCursorIndex = index;
        RefreshHighlight(startMenuOptionTexts, startCursorIndex);
        ConfirmStartMenu();
    }

    // Mouse: hover highlight for Start Menu options
    public void HoverStartOption(int index)
    {
        if (state != MenuState.StartMenu) return;
        if (index < 0 || index >= startMenuOptionTexts.Length) return;
        startCursorIndex = index;
        RefreshHighlight(startMenuOptionTexts, startCursorIndex);
    }

    // Mouse: click an option in the Pause Menu (0 = Continue, 1 = Restart, last = Back to Menu)
    public void ClickPauseOption(int index)
    {
        if (state != MenuState.PauseMenu) return;
        if (optionTexts == null || index < 0 || index >= optionTexts.Length) return;
        pauseCursorIndex = index;
        RefreshHighlight(optionTexts, pauseCursorIndex);
        ConfirmPauseMenu();
    }

    // Mouse: hover highlight for Pause Menu options
    public void HoverPauseOption(int index)
    {
        if (state != MenuState.PauseMenu) return;
        if (optionTexts == null || index < 0 || index >= optionTexts.Length) return;
        pauseCursorIndex = index;
        RefreshHighlight(optionTexts, pauseCursorIndex);
    }

    // Last = Play, second-to-last = Map (when present), others = difficulty
    void ConfirmStartMenu()
    {
        PlayClickSound();

        int len = startMenuOptionTexts.Length;
        bool isPlayOption = startCursorIndex == len - 1;
        bool isMapOption = (len >= 5 && startCursorIndex == len - 2);

        if (isPlayOption)
        {
            state = MenuState.Playing;
            if (startMenuPanel != null) startMenuPanel.SetActive(false);
            SetHudVisible(true);
            spawnManager.BeginGame();
        }
        else if (isMapOption)
        {
            if (mapManager == null) mapManager = FindAnyObjectByType<MapManager>(FindObjectsInactive.Include);
            if (mapManager != null) mapManager.CycleMapWithTransition(1);
            RefreshHighlight(startMenuOptionTexts, startCursorIndex);
        }
        else
        {
            // เลือกความซับซ้อนไว้ก่อน ยังไม่เริ่มเกม ต้องเลื่อน cursor ไปกด Play อีกที
            GameManager.Instance.complexity = startCursorIndex + 1;
        }
    }

    int MaxDifficultyIndex()
    {
        // Easy/Normal/Hard occupy 0..2 when Map+Play are present (len>=5), else 0..len-2
        int len = (startMenuOptionTexts != null) ? startMenuOptionTexts.Length : 0;
        if (len >= 5) return Mathf.Clamp(2, 0, len - 3);
        return Mathf.Max(0, len - 2);
    }

    public void RefreshMapLabel()
    {
        if (startMenuOptionTexts == null) return;
        int len = startMenuOptionTexts.Length;
        if (len < 5) return;
        var t = startMenuOptionTexts[len - 2];
        if (t == null) return;
        if (mapManager == null) mapManager = FindAnyObjectByType<MapManager>(FindObjectsInactive.Include);
        string name = (mapManager != null) ? mapManager.GetSelectedMapName() : "SKY";
        t.text = FriendLanguageUI.T("MAP: ", "ฉาก: ") + FriendLanguageUI.MapName(name.ToUpper());
    }

    void EnterPause()
    {
        state = MenuState.PauseMenu;
        Time.timeScale = 0f;
        pauseCursorIndex = 0;
        if (pausePanel != null) pausePanel.SetActive(true);
        SetHudVisible(false);
        RefreshHighlight(optionTexts, pauseCursorIndex);
    }

    // Element แรก = "Continue" / ตรงกลาง = "Restart" / Element สุดท้าย = "Back to Menu"
    void ConfirmPauseMenu()
    {
        PlayClickSound();

        bool isBackToStart = pauseCursorIndex == optionTexts.Length - 1;
        bool isRestart = (optionTexts.Length >= 3 && pauseCursorIndex == 1);

        if (isBackToStart)
        {
            BackToStartMenu();
        }
        else if (isRestart)
        {
            RestartGame();
        }
        else
        {
            ResumeGame();
        }
    }

    // Start a fresh round right away, keeping the current map / difficulty.
    void RestartGame()
    {
        Time.timeScale = 1f;
        if (pausePanel != null) pausePanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (startMenuPanel != null) startMenuPanel.SetActive(false);

        state = MenuState.Playing;
        SetHudVisible(true);
        pauseCursorIndex = 0;

        spawnManager.RestartGame();
    }

    void ResumeGame()
    {
        state = MenuState.Playing;
        Time.timeScale = 1f;
        if (pausePanel != null) pausePanel.SetActive(false);
        SetHudVisible(true);
    }

    void BackToStartMenu()
    {
        Time.timeScale = 1f;
        if (pausePanel != null) pausePanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);

        spawnManager.StopGame();

        state = MenuState.StartMenu;
        startCursorIndex = Mathf.Clamp(GameManager.Instance.complexity - 1, 0, MaxDifficultyIndex());
        if (startMenuPanel != null) startMenuPanel.SetActive(true);
        SetHudVisible(false);
        RefreshMapLabel();
        RefreshHighlight(startMenuOptionTexts, startCursorIndex);
    }
}
