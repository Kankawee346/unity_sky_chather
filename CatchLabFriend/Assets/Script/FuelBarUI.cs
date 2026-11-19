using UnityEngine;
using UnityEngine.UI;

public class FuelBarUI : MonoBehaviour
{
    [Header("Segments (left to right, each is the fill image of a cell)")]
    public Image[] segments;

    [Header("Colors")]
    public Color fullColor = new Color(0.45f, 0.95f, 0.25f, 1f);
    public Color midColor = new Color(0.95f, 0.85f, 0.2f, 1f);
    public Color lowColor = new Color(0.95f, 0.2f, 0.2f, 1f);
    [Range(0f, 1f)] public float midThreshold = 0.5f;
    [Range(0f, 1f)] public float lowThreshold = 0.25f;

    [Header("Animation")]
    [Tooltip("How fast the bar visually catches up when fuel increases.")]
    public float smoothIncreaseSpeed = 6f;
    [Tooltip("Stepped 'chunk drop' when fuel decreases (retro life-bar feel).")]
    public bool useChunkDrop = true;
    [Tooltip("Seconds between each chunk drop step.")]
    public float chunkDropInterval = 0.07f;
    [Tooltip("Size of each drop step as a fraction of the whole bar. 0 = one segment per step.")]
    [Range(0f, 1f)] public float chunkSizeOverride = 0f;

    [Header("Low Fuel Pulse")]
    public bool pulseWhenLow = true;
    public float pulseSpeed = 8f;
    public float pulseMinAlpha = 0.45f;

    float target01 = 1f;
    float display01 = 1f;
    float dropTimer = 0f;
    int lastLitSegments = -1;
    float flashTimer = 0f;
    float lastApplied01 = -1f;
    bool lastPulsing = false;

    void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnFuelChanged += OnFuelChanged;
            target01 = display01 = GameManager.Instance.maxFuel > 0f ? Mathf.Clamp01(GameManager.Instance.fuel / GameManager.Instance.maxFuel) : 0f;
            lastLitSegments = Mathf.CeilToInt(display01 * SegmentCount());
            Apply(display01, false);
        }
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null) GameManager.Instance.OnFuelChanged -= OnFuelChanged;
    }

    void OnFuelChanged(float current, float max)
    {
        target01 = max > 0f ? Mathf.Clamp01(current / max) : 0f;
    }

    int SegmentCount()
    {
        return (segments != null && segments.Length > 0) ? segments.Length : 10;
    }

    void Update()
    {
        if (flashTimer > 0f) flashTimer -= Time.deltaTime;

        if (Mathf.Abs(display01 - target01) > 0.0005f)
        {
            if (target01 < display01 && useChunkDrop)
            {
                // Stepped chunk drop animation: reduce in discrete chunk steps
                dropTimer -= Time.deltaTime;
                if (dropTimer <= 0f)
                {
                    float step = chunkSizeOverride > 0f ? chunkSizeOverride : (1f / SegmentCount());
                    display01 = Mathf.Max(target01, SnapToStep(display01 - step, step));
                    dropTimer = chunkDropInterval;
                    flashTimer = chunkDropInterval * 0.9f; // brief flash on the dropping edge
                }
            }
            else if (target01 < display01)
            {
                display01 = Mathf.MoveTowards(display01, target01, smoothIncreaseSpeed * 2f * Time.deltaTime);
            }
            else
            {
                display01 = Mathf.MoveTowards(display01, target01, smoothIncreaseSpeed * Time.deltaTime);
            }
        }
        else
        {
            display01 = target01;
            dropTimer = 0f;
        }

        bool pulsing = pulseWhenLow && display01 <= lowThreshold && display01 > 0f;
        bool flashing = flashTimer > 0f;
        if (Mathf.Abs(display01 - lastApplied01) > 0.0005f || pulsing || flashing || pulsing != lastPulsing)
        {
            Apply(display01, flashing);
            lastApplied01 = display01;
            lastPulsing = pulsing;
        }
    }

    float SnapToStep(float value, float step)
    {
        if (step <= 0f) return value;
        return Mathf.Round(value / step) * step;
    }

    void Apply(float value01, bool droppingFlash)
    {
        if (segments == null || segments.Length == 0) return;

        Color baseColor = value01 <= lowThreshold ? lowColor : (value01 <= midThreshold ? midColor : fullColor);
        if (pulseWhenLow && value01 <= lowThreshold && value01 > 0f)
        {
            float a = Mathf.Lerp(pulseMinAlpha, 1f, (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f);
            baseColor.a = a;
        }

        int total = segments.Length;
        float filled = value01 * total;
        int firstEmpty = Mathf.Clamp(Mathf.FloorToInt(filled), 0, total - 1);

        for (int i = 0; i < segments.Length; i++)
        {
            var img = segments[i];
            if (img == null) continue;
            float cell = Mathf.Clamp01(filled - i); // 0..1 for this segment
            bool isEmpty = cell <= 0.001f;
            img.fillAmount = isEmpty ? 0f : cell;

            // empty/dropped chunks reveal the gray cell background (no fuel color)
            Color c = baseColor;
            if (isEmpty)
            {
                c.a = 0f;
            }
            else if (droppingFlash && i == firstEmpty && cell < 1f)
            {
                c = Color.Lerp(baseColor, Color.white, 0.55f); // flash on the dropping edge
            }
            img.color = c;
            img.enabled = true;
        }

        lastLitSegments = Mathf.CeilToInt(filled);
    }
}
