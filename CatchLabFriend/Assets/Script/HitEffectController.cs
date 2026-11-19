using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Visual feedback when the plane is hit by an obstacle:
// red screen flash, player blink, an impact ring and a short camera shake.
// Put this on the Canvas (or anywhere); it auto-finds the player and camera.
public class HitEffectController : MonoBehaviour
{
    [Header("Screen flash")]
    [Tooltip("Full-screen red image. Auto-created under the Canvas if left empty.")]
    public Image flashOverlay;
    public Color flashColor = new Color(1f, 0.12f, 0.08f, 0.32f);
    public float flashDuration = 0.3f;

    [Header("Player blink")]
    public Color playerFlashColor = new Color(1f, 0.25f, 0.2f, 1f);
    public float blinkDuration = 0.4f;
    [Min(1)] public int blinkCount = 4;

    [Header("Impact ring")]
    public Color ringColor = new Color(1f, 0.45f, 0.25f, 0.9f);
    public int ringSortingOrder = 10;
    public float ringDuration = 0.35f;
    public float ringStartScale = 0.3f;
    public float ringEndScale = 2.2f;

    [Header("Camera shake")]
    public float shakeDuration = 0.2f;
    public float shakeMagnitude = 0.12f;
    public Camera targetCamera;

    [Header("Hit sound")]
    public AudioClip hitSound;
    [Range(0f, 1f)] public float hitVolume = 1f;

    private Sprite ringSprite;
    private Coroutine flashRoutine;
    private Coroutine blinkRoutine;
    private Coroutine shakeRoutine;
    private AudioSource hitAudioSource;
    private Vector3 cameraRestLocalPosition;
    private bool cameraRestCaptured;

    void Awake()
    {
        if (flashOverlay == null) flashOverlay = BuildFlashOverlay();
        if (flashOverlay != null)
        {
            flashOverlay.raycastTarget = false;
            flashOverlay.transform.SetAsLastSibling();
            flashOverlay.color = new Color(flashColor.r, flashColor.g, flashColor.b, 0f);
        }

        ringSprite = BuildRingSprite();
        if (targetCamera == null) targetCamera = Camera.main;
        CaptureCameraRestPosition();

        hitAudioSource = GetComponent<AudioSource>();
        if (hitAudioSource == null) hitAudioSource = gameObject.AddComponent<AudioSource>();
        hitAudioSource.playOnAwake = false;
        hitAudioSource.spatialBlend = 0f; // 2D impact sound
    }

    void Start()
    {
        if (GameManager.Instance != null) GameManager.Instance.OnHit += PlayHitEffect;
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null) GameManager.Instance.OnHit -= PlayHitEffect;
        RestoreCameraPosition();
        if (ringSprite != null)
        {
            if (ringSprite.texture != null) Destroy(ringSprite.texture);
            Destroy(ringSprite);
        }
    }

    void OnDisable()
    {
        if (shakeRoutine != null)
        {
            StopCoroutine(shakeRoutine);
            shakeRoutine = null;
        }
        RestoreCameraPosition();
    }

    public void PlayHitEffect()
    {
        // Hit sound
        if (hitSound != null && hitAudioSource != null)
            hitAudioSource.PlayOneShot(hitSound, hitVolume);

        // Screen flash
        if (flashOverlay != null)
        {
            if (flashRoutine != null) StopCoroutine(flashRoutine);
            flashRoutine = StartCoroutine(FlashRoutine());
        }

        // Player blink
        if (blinkRoutine != null) StopCoroutine(blinkRoutine);
        blinkRoutine = StartCoroutine(BlinkRoutine());

        // Impact ring at the plane
        var player = FindAnyObjectByType<PlayerController>(FindObjectsInactive.Include);
        if (player != null && ringSprite != null) SpawnRing(player.transform.position);

        // Small camera shake
        if (shakeRoutine != null)
        {
            StopCoroutine(shakeRoutine);
            RestoreCameraPosition();
        }
        shakeRoutine = StartCoroutine(ShakeRoutine());
    }

    IEnumerator FlashRoutine()
    {
        float t = 0f;
        while (t < flashDuration)
        {
            t += Time.unscaledDeltaTime;
            float k = 1f - Mathf.Clamp01(t / flashDuration);
            flashOverlay.color = new Color(flashColor.r, flashColor.g, flashColor.b, flashColor.a * k);
            yield return null;
        }
        flashOverlay.color = new Color(flashColor.r, flashColor.g, flashColor.b, 0f);
        flashRoutine = null;
    }

    IEnumerator BlinkRoutine()
    {
        var player = FindAnyObjectByType<PlayerController>(FindObjectsInactive.Include);
        if (player == null) { blinkRoutine = null; yield break; }

        var renderers = player.GetComponentsInChildren<SpriteRenderer>(true);
        if (renderers.Length == 0) { blinkRoutine = null; yield break; }

        var baseColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++) baseColors[i] = renderers[i].color;

        float step = Mathf.Max(0.02f, blinkDuration / Mathf.Max(1, blinkCount));
        for (int blink = 0; blink < blinkCount; blink++)
        {
            bool on = (blink % 2) == 0;
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null) continue;
                renderers[i].color = on ? playerFlashColor : baseColors[i];
            }
            yield return new WaitForSecondsRealtime(step);
        }

        for (int i = 0; i < renderers.Length; i++)
            if (renderers[i] != null) renderers[i].color = baseColors[i];

        blinkRoutine = null;
    }

    IEnumerator ShakeRoutine()
    {
        if (targetCamera == null) targetCamera = Camera.main;
        if (targetCamera == null) { shakeRoutine = null; yield break; }
        CaptureCameraRestPosition();

        Transform cam = targetCamera.transform;
        Vector3 basePos = cameraRestLocalPosition;

        float t = 0f;
        while (t < shakeDuration)
        {
            t += Time.unscaledDeltaTime;
            float damping = 1f - Mathf.Clamp01(t / shakeDuration);
            Vector2 offset = Random.insideUnitCircle * shakeMagnitude * damping;
            cam.localPosition = basePos + new Vector3(offset.x, offset.y, 0f);
            yield return null;
        }

        cam.localPosition = basePos;
        shakeRoutine = null;
    }

    void CaptureCameraRestPosition()
    {
        if (targetCamera == null || cameraRestCaptured) return;
        cameraRestLocalPosition = targetCamera.transform.localPosition;
        cameraRestCaptured = true;
    }

    void RestoreCameraPosition()
    {
        if (targetCamera != null && cameraRestCaptured)
            targetCamera.transform.localPosition = cameraRestLocalPosition;
    }

    void SpawnRing(Vector3 position)
    {
        var go = new GameObject("HitRing");
        go.transform.position = position;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = ringSprite;
        sr.sortingOrder = ringSortingOrder;
        sr.color = ringColor;

        StartCoroutine(AnimateRing(go, sr));
    }

    IEnumerator AnimateRing(GameObject go, SpriteRenderer sr)
    {
        float t = 0f;
        while (t < ringDuration && go != null && sr != null)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / ringDuration);
            float scale = Mathf.Lerp(ringStartScale, ringEndScale, k);
            go.transform.localScale = new Vector3(scale, scale, 1f);

            Color c = ringColor;
            c.a *= 1f - k;
            sr.color = c;
            yield return null;
        }

        if (go != null) Destroy(go);
    }

    Image BuildFlashOverlay()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindAnyObjectByType<Canvas>(FindObjectsInactive.Include);
        if (canvas == null) return null;

        var go = new GameObject("HitFlashOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(canvas.transform, false);

        var rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var img = go.GetComponent<Image>();
        img.color = new Color(flashColor.r, flashColor.g, flashColor.b, 0f);
        img.raycastTarget = false;
        go.transform.SetAsLastSibling();
        return img;
    }

    Sprite BuildRingSprite()
    {
        const int size = 64;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.name = "HitRingTexture";
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        var pixels = new Color32[size * size];
        float center = (size - 1) * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x - center) / center;
                float dy = (y - center) / center;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                // thin ring near the edge with a soft falloff
                float a = Mathf.Clamp01(1f - Mathf.Abs(d - 0.82f) / 0.18f);
                a *= a;
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
        }

        tex.SetPixels32(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
    }
}
