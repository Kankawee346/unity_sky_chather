using UnityEngine;

// Vertical parallax sky for the "items fall from the top" layout.
// The Craftpix sky layers only tile SIDEWAYS. Use the pre-baked "_Cloud_Tall" PNGs for the
// cloud layers (already mirrored + sheared so the loop is seamless AND not symmetric) and the
// original sky PNG as the back layer. Leave Mirror Alternate Tiles OFF for those.
// Attach to an empty GameObject (e.g. "Background"). It builds its own sprite tiles at runtime.
public class VerticalParallaxBackground : MonoBehaviour
{
    [Header("Layer sprites, ordered BACK to FRONT")]
    [Tooltip("Pack numbering: the highest Layer_N is the back sky, Layer_1 is the front clouds. So drag Layer_N first and Layer_1 last.")]
    public Sprite[] layers;

    [Header("Speed")]
    [Tooltip("On = scroll speed follows the game speed level (same values as the falling items)")]
    public bool followGameSpeed = true;

    [Tooltip("Scroll speed in units/sec, only used when Follow Game Speed is off")]
    public float fixedSpeed = 3f;

    [Tooltip("Back layer speed relative to the item fall speed (0 = static sky)")]
    public float backLayerFactor = 0f;

    [Tooltip("Front layer speed relative to the item fall speed (1 = same speed as the items)")]
    public float frontLayerFactor = 1f;

    [Tooltip("How quickly the scroll speed eases to a new value after a speed change (units/sec per sec)")]
    public float speedEasing = 4f;

    [Header("Drawing")]
    [Tooltip("Sorting order of the back layer. Keep it below 0 so the player and items draw in front")]
    public int sortingOrderStart = -100;

    [Tooltip("OFF for the baked _Cloud_Tall sprites (they already loop vertically). ON only for raw pack layers: every second copy is flipped, which is seamless but looks symmetric")]
    public bool mirrorAlternateTiles = false;

    [Tooltip("Scale tiles uniformly so they cover the camera view (useful when source sprites are smaller than the screen)")]
    public bool coverScreen = false;

    [Header("Opacity")]
    [Tooltip("Per-layer alpha (0 = transparent, 1 = opaque). If empty or shorter than layers, missing entries default to 1.")]
    public float[] layerAlpha;

    [Header("Tint")]
    [Tooltip("Multiplied into every layer's color. Use darker values to dim the background without affecting the player or gameplay objects.")]
    public Color colorTint = Color.white;

    private float[] offsets;
    private float[] tileHeights;
    private SpriteRenderer[][] tiles;
    private float currentSpeed;
    private bool speedInitialized = false;
    private Camera cachedCam;

    void Start()
    {
        Build();
    }

    void Build()
    {
        // clear any previously built runtime tiles (supports runtime map swaps)
        for (int c = transform.childCount - 1; c >= 0; c--)
        {
            var child = transform.GetChild(c).gameObject;
            if (Application.isPlaying) Destroy(child); else DestroyImmediate(child);
        }
        offsets = null;
        tileHeights = null;
        tiles = null;

        if (layers == null || layers.Length == 0)
        {
            Debug.LogWarning("VerticalParallaxBackground: no layer sprites assigned.");
            enabled = false;
            return;
        }

        int n = layers.Length;
        offsets = new float[n];
        tileHeights = new float[n];
        tiles = new SpriteRenderer[n][];

        for (int i = 0; i < n; i++)
        {
            if (layers[i] == null)
            {
                Debug.LogWarning("VerticalParallaxBackground: layer " + i + " is empty.");
                enabled = false;
                return;
            }

            float scale = 1f;
            if (cachedCam == null) cachedCam = Camera.main;
            if (coverScreen && cachedCam != null)
            {
                float viewH = cachedCam.orthographicSize * 2f;
                float viewW = viewH * ((Screen.width > 0 && Screen.height > 0) ? (float)Screen.width / Screen.height : cachedCam.aspect);
                float sx = layers[i].bounds.size.x > 0.001f ? viewW / layers[i].bounds.size.x : 1f;
                float sy = layers[i].bounds.size.y > 0.001f ? viewH / layers[i].bounds.size.y : 1f;
                scale = Mathf.Max(sx, sy, 0.001f); // cover the full view
            }

            tileHeights[i] = layers[i].bounds.size.y * scale;

            float alpha = 1f;
            if (layerAlpha != null && i < layerAlpha.Length)
                alpha = Mathf.Clamp01(layerAlpha[i]);
            Color tint = colorTint;
            tint.a *= alpha;

            GameObject layerObject = new GameObject("Layer " + i);
            layerObject.transform.SetParent(transform, false);

            // 3 tiles are enough to cover the view while one wraps around
            tiles[i] = new SpriteRenderer[3];
            for (int j = 0; j < 3; j++)
            {
                GameObject tileObject = new GameObject("Tile " + j);
                tileObject.transform.SetParent(layerObject.transform, false);

                SpriteRenderer sr = tileObject.AddComponent<SpriteRenderer>();
                sr.sprite = layers[i];
                sr.sortingOrder = sortingOrderStart + i;
                sr.color = tint;
                sr.transform.localScale = Vector3.one * scale;
                tiles[i][j] = sr;
            }
        }

        enabled = true;
        // keep scrolling continuous on swap
        speedInitialized = false;
    }

    // Swap the parallax layer set at runtime (e.g. when changing maps)
    public void SetLayers(Sprite[] newLayers)
    {
        layers = newLayers;
        Build();
    }

    void Update()
    {
        if (layers == null || layers.Length == 0 || tiles == null || tileHeights == null || offsets == null) return;
        if (tiles.Length != layers.Length) return; // mid-rebuild safety

        float targetSpeed = fixedSpeed;
        if (followGameSpeed && GameManager.Instance != null)
        {
            targetSpeed = SpawnManager.EffectiveFallSpeed(GameManager.Instance.level, GameManager.Instance.complexity);
        }

        if (!speedInitialized)
        {
            currentSpeed = targetSpeed;
            speedInitialized = true;
        }

        // Time.deltaTime is 0 while paused (timeScale = 0), so the sky freezes with the game
        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, speedEasing * Time.deltaTime);

        if (cachedCam == null) cachedCam = Camera.main;
        Vector3 cam = cachedCam != null ? cachedCam.transform.position : Vector3.zero;
        int n = layers.Length;

        for (int i = 0; i < n; i++)
        {
            // i = 0 is the back layer, i = n-1 is the front layer
            float t = n > 1 ? (float)i / (n - 1) : 1f;
            float factor = Mathf.Lerp(backLayerFactor, frontLayerFactor, t);

            float h = tileHeights[i];
            offsets[i] += currentSpeed * factor * Time.deltaTime;

            // Wrap by 2 tile heights so the flip pattern (which repeats every 2 tiles) stays continuous
            if (offsets[i] >= 2f * h) offsets[i] -= 2f * h;

            int tileIndex = Mathf.FloorToInt(offsets[i] / h);
            float remainder = offsets[i] - tileIndex * h;

            for (int j = -1; j <= 1; j++)
            {
                SpriteRenderer sr = tiles[i][j + 1];
                // The world moves DOWN, so the tile centres move down as the offset grows
                sr.transform.position = new Vector3(cam.x, cam.y - remainder + j * h, 0f);
                sr.flipY = mirrorAlternateTiles && (((tileIndex + j) & 1) != 0);
            }
        }
    }
}
