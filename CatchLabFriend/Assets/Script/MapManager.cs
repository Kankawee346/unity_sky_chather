using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class MapManager : MonoBehaviour
{
    [System.Serializable]
    public class MapConfig
    {
        public string displayName = "Sky";
        [Tooltip("Hierarchy object with the VerticalParallaxBackground for this map (its own layer/speed settings)")]
        public GameObject backgroundObject;
        [Tooltip("Falling object prefabs to spawn on this map")]
        public GameObject[] objectPrefabs;
        [Tooltip("Player sprite for this map (keeps current animator/smoke if null)")]
        public Sprite playerSprite;
        [Tooltip("Optional animator controller for this map (keeps current if null, and animator is enabled/kept when Use Player Animator is true)")]
        public RuntimeAnimatorController playerAnimatorController;
        [Tooltip("Enable the player's Animator for this map (disable for static sprites like the rocket)")]
        public bool usePlayerAnimator = true;
        [Tooltip("Enable the SmokeTrail particle child for this map")]
        public bool enableSmokeTrail = true;
        [Tooltip("Enable the RocketFlame particle child for this map")]
        public bool enableRocketFlame = false;
        [Tooltip("Override player transform local scale for this map (keeps visual size consistent across different sprites)")]
        public bool overridePlayerScale = false;
        public Vector3 playerScale = new Vector3(0.35f, 0.35f, 1f);
        [Tooltip("Camera clear/background color for this map")]
        public Color cameraBackground = new Color(0.192f, 0.302f, 0.475f, 1f);
        [Tooltip("Start menu panel tint for this map (use white when showing a custom sprite)")]
        public Color menuPanelColor = new Color(0f, 0f, 0f, 0f);
        [Tooltip("Start menu panel background sprite for this map (null = keep unchanged)")]
        public Sprite menuPanelSprite;
        [Tooltip("Image type for the start menu panel sprite")]
        public Image.Type menuPanelImageType = Image.Type.Sliced;
    }

    [Header("References")]
    public SpawnManager spawnManager;
    public SpriteRenderer playerRenderer;
    public Animator playerAnimator;
    public Image startMenuPanelImage;

    [Header("Transition (optional)")]
    public Image fadeOverlay;
    public float fadeDuration = 0.2f;

    [Header("Maps (0 = default/sky)")]
    public MapConfig[] maps;

    [Header("Selection (persisted)")]
    public int selectedMap = 0;
    const string PrefKey = "selected_map";
    bool transitioning = false;

    void Awake()
    {
        selectedMap = Mathf.Clamp(PlayerPrefs.GetInt(PrefKey, selectedMap), 0, maps != null && maps.Length > 0 ? maps.Length - 1 : 0);
        if (fadeOverlay != null)
        {
            var c = fadeOverlay.color; c.a = 0f; fadeOverlay.color = c;
            fadeOverlay.raycastTarget = false;
            fadeOverlay.gameObject.SetActive(true);
        }
    }

    void Start()
    {
        // apply persisted map so the menu/background match the saved choice
        ApplySelectedMap();
    }

    public void SetMap(int index)
    {
        if (maps == null || maps.Length == 0) return;
        selectedMap = Mathf.Clamp(index, 0, maps.Length - 1);
        PlayerPrefs.SetInt(PrefKey, selectedMap);
        PlayerPrefs.Save();
        ApplySelectedMap();
    }

    public void CycleMap(int direction)
    {
        if (maps == null || maps.Length == 0) return;
        SetMap((selectedMap + direction + maps.Length) % maps.Length);
    }

    public void CycleMapWithTransition(int direction)
    {
        if (maps == null || maps.Length == 0 || transitioning) return;
        int target = (selectedMap + direction + maps.Length) % maps.Length;
        StartCoroutine(TransitionTo(target));
    }

    IEnumerator TransitionTo(int index)
    {
        transitioning = true;
        if (fadeOverlay != null)
        {
            fadeOverlay.transform.SetAsLastSibling();
            fadeOverlay.raycastTarget = true;
            yield return Fade(0f, 1f);
        }
        SetMap(index);
        var pm = FindAnyObjectByType<PauseMenuController>(FindObjectsInactive.Include);
        if (pm != null) pm.RefreshMapLabel();
        // allow a frame for activation/build to settle
        yield return null;
        if (fadeOverlay != null)
        {
            yield return Fade(1f, 0f);
            fadeOverlay.raycastTarget = false;
        }
        transitioning = false;
    }

    IEnumerator Fade(float from, float to)
    {
        float d = Mathf.Max(0.01f, fadeDuration);
        float t = 0f;
        while (t < d)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Lerp(from, to, Mathf.Clamp01(t / d));
            var c = fadeOverlay.color; c.a = a; fadeOverlay.color = c;
            yield return null;
        }
        var end = fadeOverlay.color; end.a = to; fadeOverlay.color = end;
    }

    public string GetSelectedMapName()
    {
        if (maps == null || maps.Length == 0) return "MAP";
        var m = maps[Mathf.Clamp(selectedMap, 0, maps.Length - 1)];
        return string.IsNullOrEmpty(m.displayName) ? ("MAP " + (selectedMap + 1)) : m.displayName;
    }

    public void ApplySelectedMap()
    {
        if (maps == null || maps.Length == 0) return;
        ApplyMap(Mathf.Clamp(selectedMap, 0, maps.Length - 1));
    }

    public void ApplyMap(int index)
    {
        if (maps == null || index < 0 || index >= maps.Length) return;
        var m = maps[index];

        // Toggle map-specific background hierarchy objects (each has its own parallax settings)
        for (int i = 0; i < maps.Length; i++)
        {
            if (maps[i] != null && maps[i].backgroundObject != null)
                maps[i].backgroundObject.SetActive(i == index);
        }

        if (Camera.main != null) Camera.main.backgroundColor = m.cameraBackground;

        if (startMenuPanelImage == null)
        {
            var panel = GameObject.Find("StartMenuPanel");
            if (panel != null) startMenuPanelImage = panel.GetComponent<Image>();
        }
        if (startMenuPanelImage != null)
        {
            if (m.menuPanelSprite != null)
            {
                startMenuPanelImage.sprite = m.menuPanelSprite;
                startMenuPanelImage.type = m.menuPanelImageType;
                startMenuPanelImage.preserveAspect = false;
            }
            startMenuPanelImage.color = m.menuPanelColor;
        }

        if (spawnManager == null) spawnManager = FindAnyObjectByType<SpawnManager>(FindObjectsInactive.Include);
        if (spawnManager != null && m.objectPrefabs != null && m.objectPrefabs.Length > 0)
            spawnManager.objectPrefabs = System.Array.FindAll(m.objectPrefabs, prefab =>
                prefab != null && prefab.GetComponent<FallingObject>() != null &&
                prefab.GetComponent<FallingObject>().itemType != FallingObject.ItemType.Obstacle);

        var pc2 = FindAnyObjectByType<PlayerController>(FindObjectsInactive.Include);
        if (playerRenderer == null && pc2 != null) playerRenderer = pc2.GetComponentInChildren<SpriteRenderer>(true);
        if (playerAnimator == null && pc2 != null) playerAnimator = pc2.GetComponentInChildren<Animator>(true);
        if (pc2 != null && m.overridePlayerScale)
            pc2.transform.localScale = m.playerScale;
        if (playerRenderer != null && m.playerSprite != null)
            playerRenderer.sprite = m.playerSprite;
        if (playerAnimator != null)
        {
            if (!m.usePlayerAnimator)
            {
                playerAnimator.enabled = false;
            }
            else
            {
                playerAnimator.enabled = true;
                if (m.playerAnimatorController != null)
                    playerAnimator.runtimeAnimatorController = m.playerAnimatorController;
            }
        }
        if (pc2 != null)
        {
            var smoke = pc2.transform.Find("SmokeTrail");
            if (smoke != null) smoke.gameObject.SetActive(m.enableSmokeTrail);
            var flame = pc2.transform.Find("RocketFlame");
            if (flame != null) flame.gameObject.SetActive(m.enableRocketFlame);
        }
    }
}
