using UnityEngine;
using UnityEngine.InputSystem;

public sealed class CatchGame : MonoBehaviour
{
    private const float BasketY = -240f;
    private const float ItemSize = 32f;
    private const float SensorTimeout = 0.35f;
    private const float RoundSeconds = 30f;
    private static readonly float[] FallSpeeds = { 145f, 275f, 425f };
    private static readonly string[] Praise = { "", "", "OK", "GOOD", "GREAT", "EXCELLENT" };

    private RectTransform board;
    private RectTransform basket;
    private RectTransform item;
    private UnityEngine.UI.Image itemImage;
    private CatchLabUI ui;
    public bool Playing { get; private set; }
    public bool Paused { get; private set; }
    public int Level => level;
    public int Score => score;
    public int Combo => combo;
    public int Caught => itemsCaught;
    public int Seen => itemsSeen;
    public int Hearts { get; private set; } = 5;
    public int RelicKind { get; private set; }
    public float Remaining => remaining;
    public bool Joystick => useJoystick;
    public bool SensorOnline => Time.unscaledTime - sampleTime <= SensorTimeout;
    public string SaveState { get; private set; } = "";
    public string PraiseText => Praise[Mathf.Min(combo, Praise.Length - 1)];
    private bool useJoystick;
    private bool leftActive;
    private bool rightActive;
    private float sampleTime = float.NegativeInfinity;
    private int level;
    private int score;
    private int combo;
    private int itemsCaught;
    private int itemsSeen;
    private string roundId;
    private int itemPoints;
    private float itemDrift;
    private float flightTime;
    private float remaining;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void StartGame()
    {
        new GameObject(nameof(CatchGame), typeof(RectTransform), typeof(CatchGame));
    }

    private void Awake()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = gameObject.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1440, 900);
        scaler.screenMatchMode = UnityEngine.UI.CanvasScaler.ScreenMatchMode.Expand;
        gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        if (FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem),
                typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
        }

        ui = gameObject.AddComponent<CatchLabUI>();
        board = ui.Build(this, canvas.transform);
        basket = Panel("Basket", board, new Vector2(106, 18), new Vector2(0, BasketY),
            new Color32(98, 235, 199, 255)).rectTransform;
        itemImage = Panel("FallingItem", board, Vector2.one * ItemSize, Vector2.zero, Color.white);
        item = itemImage.rectTransform;

        ui.DressPlayer(basket, itemImage);
        ui.Home();
        gameObject.AddComponent<ControllerReceiver>();
        gameObject.AddComponent<GameStatusSender>();
    }

    public void SelectSensor(bool joystick)
    {
        if (useJoystick == joystick)
            return;

        useJoystick = joystick;
        sampleTime = float.NegativeInfinity;
    }

    // One board sends both FSR states and the calibrated joystick direction.
    // A fresh sample is required after a mode change.
    public void ReceiveSensorSample(bool leftFsrPressed, bool rightFsrPressed, int joystickDirection)
    {
        leftActive = useJoystick ? joystickDirection < 0 : leftFsrPressed;
        rightActive = useJoystick ? joystickDirection > 0 : rightFsrPressed;
        sampleTime = Time.unscaledTime;
    }

    public void StartRound(int difficulty)
    {
        level = Mathf.Clamp(difficulty, 0, 2);
        Playing = true;
        Paused = false;
        SaveState = "";
        score = combo = 0;
        itemsCaught = itemsSeen = 0;
        Hearts = 5;
        roundId = System.Guid.NewGuid().ToString("N");
        remaining = RoundSeconds;
        basket.anchoredPosition = new Vector2(0, BasketY);
        SpawnItem();
        ui.ShowPlay();
    }

    public void TogglePause() { if (Playing) Paused = !Paused; }
    public void LeaveRound() { Playing = false; Paused = false; SaveState = ""; }
    public void SelectDifficulty(int difficulty) { if (!Playing) level=Mathf.Clamp(difficulty,0,2); }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame && Playing)
            TogglePause();
        if (Playing && !Paused && remaining > 0)
        {
            float delta = Mathf.Min(Time.deltaTime, remaining);
            remaining -= delta;
            MoveBasket(delta);
            MoveItem(delta);
            if (remaining <= 0)
            {
                Playing = false;
                SaveState = "saving";
                string completedRound = roundId;
                ui.ShowResults();
                StartCoroutine(RoundReporter.Save(new RoundReporter.Result
                {
                    roundId = roundId,
                    score = score,
                    level = level + 1,
                    durationMs = Mathf.RoundToInt(RoundSeconds * 1000),
                    itemsCaught = itemsCaught,
                    itemsSeen = itemsSeen
                }, success => { if (roundId == completedRound) SaveState = success ? "saved" : "failed"; }));
            }
        }

        ui.Tick();
    }

    private void MoveBasket(float delta)
    {
        Keyboard keys = Keyboard.current;
        bool sampleIsFresh = Time.unscaledTime - sampleTime <= SensorTimeout;
        bool left = leftActive && sampleIsFresh;
        bool right = rightActive && sampleIsFresh;
        if (keys != null)
        {
            left |= useJoystick ? keys.leftArrowKey.isPressed : keys.aKey.isPressed;
            right |= useJoystick ? keys.rightArrowKey.isPressed : keys.dKey.isPressed;
        }

        int direction = (right ? 1 : 0) - (left ? 1 : 0);
        float x = Mathf.Clamp(basket.anchoredPosition.x + direction * 480f * delta, -397f, 397f);
        basket.anchoredPosition = new Vector2(x, BasketY);
    }

    private void MoveItem(float delta)
    {
        Vector2 previous = item.anchoredPosition;
        flightTime += delta;
        float zigzag = level == 2 ? Mathf.Sin(flightTime * 13f) * 125f : 0f;
        Vector2 next = previous + new Vector2(itemDrift + zigzag, -FallSpeeds[level]) * delta;
        next.x = Mathf.Clamp(next.x, -380f, 380f);
        const float catchY = BasketY + (18f + ItemSize) / 2f;
        if (next.y > catchY)
        {
            item.anchoredPosition = next;
            return;
        }

        float crossingX = Mathf.Lerp(previous.x, next.x, (previous.y - catchY) / (previous.y - next.y));
        if (Mathf.Abs(crossingX - basket.anchoredPosition.x) <= (106f + ItemSize) / 2f)
        {
            AwardCatch(crossingX);
        }
        else
        {
            itemsSeen++;
            combo = 0;
            Hearts = Mathf.Max(0, Hearts - 1);
            ui.React(false, crossingX, 0);
        }
        SpawnItem();
    }

    private void AwardCatch(float x)
    {
        itemsSeen++; itemsCaught++; score += itemPoints; combo++;
        ui.React(true, x, itemPoints);
    }

    private void SpawnItem()
    {
        RelicKind = Random.Range(0, 3);
        itemPoints = new[] { 1, 2, 3 }[RelicKind];
        itemImage.color = itemPoints switch
        {
            1 => new Color32(100, 182, 255, 255),
            2 => new Color32(255, 212, 102, 255),
            _ => new Color32(249, 131, 168, 255)
        };
        ui.SetRelic(RelicKind, itemPoints);
        float x = level == 0 ? Random.Range(-2, 3) * 145f : Random.Range(-340f, 340f);
        itemDrift = level == 0 ? 0f : level == 1 ? Random.Range(-45f, 45f) : Random.Range(-165f, 165f);
        flightTime = 0f;
        item.anchoredPosition = new Vector2(x, 183f);
    }

    private static UnityEngine.UI.Image Panel(string title, Transform parent, Vector2 size,
        Vector2 position, Color color)
    {
        RectTransform rect = Place(title, parent, size, position);
        var image = rect.gameObject.AddComponent<UnityEngine.UI.Image>();
        image.color = color;
        return image;
    }

    private static UnityEngine.UI.Text Label(Transform parent, Vector2 size, Vector2 position, int fontSize)
    {
        RectTransform rect = Place("Label", parent, size, position);
        var label = rect.gameObject.AddComponent<UnityEngine.UI.Text>();
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = fontSize;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.white;
        label.raycastTarget = false;
        return label;
    }

    private static RectTransform Place(string title, Transform parent, Vector2 size, Vector2 position)
    {
        var rect = new GameObject(title, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        return rect;
    }
}
