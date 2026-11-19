using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// แนบไว้บน GameObject เดียวกับ GameManager ก็ได้ (ชื่อ "GameManager")
public class SpawnManager : MonoBehaviour
{
    [Header("Prefab ของที่ตก 1-3 แบบ ตามสเปค")]
    public GameObject[] objectPrefabs;

    [Header("ขอบเขตแนวนอนที่ของจะสุ่มตำแหน่งตกลงมา")]
    public float spawnMinX = -4f;
    public float spawnMaxX = 4f;
    public float spawnY = 6f;

    [Header("จังหวะการปล่อยของ (ค่าพื้นฐาน ก่อนคูณด้วยความซับซ้อน)")]
    public float spawnInterval = 1.2f;

    [Header("ความเร็วตกของวัตถุ (คูณเพิ่มจากความเร็วพื้นฐาน/ความยาก — ไม่กระทบความเร็วพื้นหลัง)")]
    [Tooltip("Extra multiplier applied to spawned falling objects only (background parallax speed stays unchanged).")]
    public float objectFallSpeedMultiplier = 1.4f;

    [Header("การเว้นช่องเมื่อมีสิ่งกีดขวาง")]
    [Tooltip("Minimum horizontal distance from active obstacles when spawning Fuel/Reward (keeps them out of the obstacle's row/lane).")]
    public float obstacleAvoidXDistance = 1.2f;

    [Header("Same-item spacing")]
    [Tooltip("Items sharing a Spawn Tag (FallingObject.spawnTag) will not spawn within this horizontal distance of each other while both are falling. Used for Redcoin so two never appear side by side.")]
    public float sameTagMinXDistance = 2.0f;

    [Header("หน้า Start Menu (แสดง/ซ่อนตอนเริ่ม-จบเกม)")]
    public GameObject startMenuPanel;

    [Header("Gameplay UI (score, combo, fuel...) - hidden while the start menu is shown")]
    public GameObject hudRoot;

    [Header("Player sprite (hidden in the start menu)")]
    public GameObject playerObject;

    [Header("Map system (optional)")]
    public MapManager mapManager;

    [Header("Background music (optional)")]
    [Tooltip("Resumed when the start menu is shown again so the menu and gameplay share the same music at the same volume.")]
    public BackgroundMusic backgroundMusic;

    private Coroutine spawnLoopCoroutine;
    private Vector3 playerStartPosition;
    private bool hasPlayerStartPosition;

    void SetHud(bool visible)
    {
        if (hudRoot != null) hudRoot.SetActive(visible);
    }

    void SetPlayerVisible(bool visible)
    {
        if (playerObject == null)
        {
            var pc = FindAnyObjectByType<PlayerController>(FindObjectsInactive.Include);
            if (pc != null) playerObject = pc.gameObject;
        }
        if (playerObject != null)
        {
            foreach (var r in playerObject.GetComponentsInChildren<Renderer>(true))
                r.enabled = visible;
        }
    }

    [Header("Game Over UI (shown when fuel reaches 0)")]
    public GameObject gameOverPanel;

    void Start()
    {
        if (mapManager == null) mapManager = FindAnyObjectByType<MapManager>(FindObjectsInactive.Include);
        // Out of fuel -> stop the round and show the Game Over screen
        GameManager.Instance.OnGameOver += HandleGameOver;

        // Remember where the plane sits so Restart can put it back
        if (playerObject == null)
        {
            var pc = FindAnyObjectByType<PlayerController>(FindObjectsInactive.Include);
            if (pc != null) playerObject = pc.gameObject;
        }
        if (playerObject != null)
        {
            playerStartPosition = playerObject.transform.position;
            hasPlayerStartPosition = true;
        }

        // apply persisted map (background + prefabs + player sprite) before showing menu
        if (mapManager != null) mapManager.ApplySelectedMap();

        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (startMenuPanel != null)
        {
            startMenuPanel.SetActive(true);
            SetHud(false);
            SetPlayerVisible(false);
        }
        else
        {
            // ถ้าไม่ได้ผูก Start Menu ไว้ ให้เริ่มเกมทันทีเหมือนเดิม (กันพังของเก่า)
            BeginGame();
        }
    }

    // เรียกจาก StartMenuController ตอนกดยืนยัน "Play"
    public void BeginGame()
    {
        if (mapManager == null) mapManager = FindAnyObjectByType<MapManager>(FindObjectsInactive.Include);
        if (mapManager != null) mapManager.ApplySelectedMap();

        if (startMenuPanel != null) startMenuPanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);

        SetHud(true);
        SetPlayerVisible(true);
        GameManager.Instance.StartSession();
        spawnLoopCoroutine = StartCoroutine(SpawnLoop());
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null) GameManager.Instance.OnGameOver -= HandleGameOver;
    }

    // Stop spawning and clear falling objects. Ends the session if still running.
    public void CleanupRound()
    {
        if (spawnLoopCoroutine != null)
        {
            StopCoroutine(spawnLoopCoroutine);
            spawnLoopCoroutine = null;
        }

        // Make sure the session is closed (safe if it already ended)
        GameManager.Instance.EndSession();

        // copy: Destroy triggers OnDisable which removes from the tracked list
        var activeObjects = FallingObject.Active.ToArray();
        foreach (FallingObject obj in activeObjects)
        {
            if (obj != null) Destroy(obj.gameObject);
        }
        FallingObject.Active.Clear();
    }

    // Fuel depleted: clean up the round and show the Game Over screen (not the start menu).
    void HandleGameOver()
    {
        CleanupRound();
        SetHud(false);
        SetPlayerVisible(false);
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            var ui = gameOverPanel.GetComponent<GameOverUI>();
            if (ui != null) ui.Show(GameManager.Instance.userId, GameManager.Instance.score);
        }
        else if (startMenuPanel != null)
        {
            startMenuPanel.SetActive(true); // fallback
        }
    }

    // เรียกจาก PauseMenuController ตอนกดยืนยัน "Back to Start" / ออกจากหน้า Game Over
    public void StopGame()
    {
        CleanupRound();
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (startMenuPanel != null) startMenuPanel.SetActive(true);
        SetHud(false);
        SetPlayerVisible(false);

        // Same music, same volume as during gameplay while the menu is shown
        if (backgroundMusic == null) backgroundMusic = FindAnyObjectByType<BackgroundMusic>(FindObjectsInactive.Include);
        if (backgroundMusic != null) backgroundMusic.StartMusic();
    }

    // เรียกจาก PauseMenuController ตอนกด "Restart" ในหน้า Pause
    // ล้างรอบเดิม (เก็บคะแนนรอบที่แล้วลง Leaderboard) แล้วเริ่มรอบใหม่ทันที บน map/ความยากเดิม
    public void RestartGame()
    {
        CleanupRound();

        if (hasPlayerStartPosition && playerObject != null)
            playerObject.transform.position = playerStartPosition;

        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (startMenuPanel != null) startMenuPanel.SetActive(false);

        SetHud(true);
        SetPlayerVisible(true);
        GameManager.Instance.StartSession();
        spawnLoopCoroutine = StartCoroutine(SpawnLoop());
    }

    IEnumerator SpawnLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnInterval * ComplexityIntervalMultiplier());
            SpawnOne();
        }
    }

    // ปรับความถี่ในการปล่อยของตามความซับซ้อนที่เลือกไว้หน้า Start Menu
    // Easy = ปล่อยช้าลง (มีเวลาคิดมากขึ้น) / Hard = ปล่อยถี่ขึ้น
    float ComplexityIntervalMultiplier()
    {
        switch (GameManager.Instance.complexity)
        {
            case 1: return 1.4f; // Easy
            case 3: return 0.7f; // Hard
            default: return 1f;  // Normal
        }
    }

    // ปรับความเร็วการตกของวัตถุตามความยาก (Easy = ตกช้าลง / Hard = ตกเร็วขึ้น)
    public static float ComplexitySpeedMultiplier(int complexity)
    {
        switch (complexity)
        {
            case 1: return 0.7f;  // Easy
            case 3: return 1.4f;  // Hard
            default: return 1f;   // Normal
        }
    }

    // ความเร็วตกที่ใช้จริง = ความเร็วตาม level คูณด้วยตัวคูณความยาก
    public static float EffectiveFallSpeed(int level, int complexity)
    {
        return SpeedForLevel(level) * ComplexitySpeedMultiplier(complexity);
    }

    void SpawnOne()
    {
        if (objectPrefabs == null || objectPrefabs.Length == 0) return;
        GameObject prefab = PickWeightedPrefab();
        if (prefab == null) return;
        FallingObject prefabData = prefab.GetComponent<FallingObject>();
        if (prefabData == null) return;

        GetVisibleSpawnRange(prefab, out float visibleMinX, out float visibleMaxX);
        float x = PickSpawnX(prefabData, visibleMinX, visibleMaxX);
        Vector3 spawnPos = new Vector3(x, spawnY, 0f);

        GameObject go = Instantiate(prefab, spawnPos, Quaternion.identity);

        FallingObject fo = go.GetComponent<FallingObject>();
        if (fo.itemType != FallingObject.ItemType.Obstacle)
            GameManager.Instance.RegisterSpawn();
        int lvl = 1;
        int comp = 2;
        if (GameManager.Instance != null)
        {
            lvl = GameManager.Instance.level;
            comp = GameManager.Instance.complexity;
        }
        fo.fallSpeed = EffectiveFallSpeed(lvl, comp) * Mathf.Max(0.01f, objectFallSpeedMultiplier);
    }

    // สุ่มเลือก Prefab แบบถ่วงน้ำหนักตามค่า Spawn Weight ของแต่ละชนิด
    // (ค่ามาก = ออกบ่อยกว่า) แทนการสุ่มเท่าๆ กันแบบเดิม
    GameObject PickWeightedPrefab()
    {
        int complexity = (GameManager.Instance != null) ? GameManager.Instance.complexity : 2;

        int totalWeight = 0;
        foreach (GameObject prefab in objectPrefabs)
        {
            var fo = prefab != null ? prefab.GetComponent<FallingObject>() : null;
            totalWeight += EffectiveSpawnWeight(fo, complexity);
        }
        if (totalWeight <= 0) totalWeight = 1;

        int roll = Random.Range(0, totalWeight);
        int cumulative = 0;

        foreach (GameObject prefab in objectPrefabs)
        {
            var fo = prefab != null ? prefab.GetComponent<FallingObject>() : null;
            cumulative += EffectiveSpawnWeight(fo, complexity);
            if (roll < cumulative)
            {
                return prefab;
            }
        }

        return objectPrefabs[objectPrefabs.Length - 1]; // กันพลาดเผื่อปัดเศษ
    }

    int EffectiveSpawnWeight(FallingObject fo, int complexity)
    {
        if (fo == null) return 1;
        float mult = 1f;
        switch (complexity)
        {
            case 1: mult = fo.easySpawnWeightMultiplier; break;
            case 3: mult = fo.hardSpawnWeightMultiplier; break;
            default: mult = fo.normalSpawnWeightMultiplier; break;
        }
        int w = Mathf.RoundToInt(fo.spawnWeight * Mathf.Max(0f, mult));
        return Mathf.Max(1, w);
    }

    // หาตำแหน่ง X ที่จะ spawn
    // - ถ้า needsExtraSpacing: เลือกจุดที่ห่างจากของที่ตกอยู่มากที่สุด
    // - ถ้าเป็น Fuel/Reward และมี Obstacle กำลังตก: หลีกเลี่ยงไม่ให้เกิดในแถว/เลนเดียวกับ Obstacle
    // - Items with the same Spawn Tag (e.g. Redcoin) never spawn next to each other
    void GetVisibleSpawnRange(GameObject prefab, out float minX, out float maxX)
    {
        minX = spawnMinX;
        maxX = spawnMaxX;
        Camera gameCamera = Camera.main;
        if (gameCamera == null || !gameCamera.orthographic) return;

        var renderer = prefab.GetComponentInChildren<SpriteRenderer>(true);
        float halfWidth = renderer != null && renderer.sprite != null
            ? renderer.sprite.bounds.extents.x * Mathf.Abs(renderer.transform.lossyScale.x)
            : 0.35f;
        float padding = Mathf.Max(0.35f, halfWidth + 0.15f);
        float left = gameCamera.ViewportToWorldPoint(new Vector3(0f, 0.5f)).x + padding;
        float right = gameCamera.ViewportToWorldPoint(new Vector3(1f, 0.5f)).x - padding;
        minX = Mathf.Max(minX, left);
        maxX = Mathf.Min(maxX, right);
        if (minX > maxX) minX = maxX = (left + right) * 0.5f;
    }

    float PickSpawnX(FallingObject spawning, float minX, float maxX)
    {
        bool needsSpacing = spawning != null && spawning.needsExtraSpacing;
        bool avoidObstacles = spawning != null && spawning.itemType != FallingObject.ItemType.Obstacle;
        string spawnTag = spawning != null ? spawning.spawnTag : null;
        bool avoidSameTag = !string.IsNullOrEmpty(spawnTag) && sameTagMinXDistance > 0f;

        if (!needsSpacing && !avoidObstacles && !avoidSameTag)
        {
            return Random.Range(minX, maxX);
        }

        var activeObjects = FallingObject.Active;
        if (activeObjects.Count == 0)
        {
            return Random.Range(minX, maxX);
        }

        float avoid = Mathf.Max(0f, obstacleAvoidXDistance);
        float avoidTag = Mathf.Max(0f, sameTagMinXDistance);
        float bestX = Random.Range(minX, maxX);
        float bestScore = float.NegativeInfinity;

        for (int attempt = 0; attempt < 24; attempt++)
        {
            float candidateX = Random.Range(minX, maxX);
            float minDistAll = float.MaxValue;
            float minDistObstacle = float.MaxValue;
            float minDistSameTag = float.MaxValue;
            bool nearObstacle = false;
            bool nearSameTag = false;

            for (int oi = 0; oi < activeObjects.Count; oi++)
            {
                FallingObject obj = activeObjects[oi];
                if (obj == null) continue;
                float dist = Mathf.Abs(obj.transform.position.x - candidateX);
                if (dist < minDistAll) minDistAll = dist;
                if (obj.itemType == FallingObject.ItemType.Obstacle)
                {
                    if (dist < minDistObstacle) minDistObstacle = dist;
                    if (avoidObstacles && dist < avoid) nearObstacle = true;
                }
                if (avoidSameTag && !string.IsNullOrEmpty(obj.spawnTag) && obj.spawnTag == spawnTag)
                {
                    if (dist < minDistSameTag) minDistSameTag = dist;
                    if (dist < avoidTag) nearSameTag = true;
                }
            }

            if (minDistAll == float.MaxValue) minDistAll = 999f;
            if (minDistObstacle == float.MaxValue) minDistObstacle = 999f;
            if (minDistSameTag == float.MaxValue) minDistSameTag = 999f;

            bool clear = (!avoidObstacles || !nearObstacle) && (!avoidSameTag || !nearSameTag);

            // Fuel/Reward: when an obstacle lane must be avoided, pick randomly among clear positions
            // (not maximally far from the obstacle) to prevent objects lining up. Keep only a mild
            // spacing preference for items that explicitly need extra spacing.
            float score;
            if (clear)
                score = 1000f + Random.value + (needsSpacing ? minDistAll * 0.01f : 0f);
            else
            {
                // fallback: get as far as possible from the obstacle / same-tag item in the way
                float fallback = Mathf.Min(minDistObstacle, minDistSameTag);
                score = needsSpacing ? Mathf.Min(minDistAll, fallback) : fallback;
            }

            if (score > bestScore)
            {
                bestScore = score;
                bestX = candidateX;
            }
        }

        // Guarantee the same-tag rule: if the random attempts never found a spot far enough,
        // scan the whole spawn range and pick the position furthest from the same-tag item(s).
        if (avoidSameTag)
        {
            float bestTagDist = MinSameTagDistance(bestX, spawnTag, activeObjects);
            if (bestTagDist < avoidTag)
            {
                const int samples = 61;
                float stepX = (maxX - minX) / (samples - 1);
                for (int i = 0; i < samples; i++)
                {
                    float x = minX + stepX * i;
                    float d = MinSameTagDistance(x, spawnTag, activeObjects);
                    if (d > bestTagDist)
                    {
                        bestTagDist = d;
                        bestX = x;
                    }
                }
            }
        }

        return bestX;
    }

    // Distance to the closest active item that shares the same spawn tag, or 999 if none.
    float MinSameTagDistance(float x, string tag, List<FallingObject> activeObjects)
    {
        float min = float.MaxValue;
        for (int i = 0; i < activeObjects.Count; i++)
        {
            FallingObject obj = activeObjects[i];
            if (obj == null) continue;
            if (string.IsNullOrEmpty(obj.spawnTag) || obj.spawnTag != tag) continue;
            float d = Mathf.Abs(obj.transform.position.x - x);
            if (d < min) min = d;
        }
        return min == float.MaxValue ? 999f : min;
    }

    // ผูกความเร็วกับ level 1-3 ตามสเปค ปรับตัวเลขได้ตามความเหมาะสมตอนทดสอบจริง
    // เป็น public static เพื่อให้ PauseMenuController เรียกใช้ค่าชุดเดียวกันได้ ไม่ต้องพิมพ์ตัวเลขซ้ำ
    public static float SpeedForLevel(int level)
    {
        switch (level)
        {
            case 1: return 2f;
            case 2: return 3f;
            case 3: return 6f;
            default: return 2f;
        }
    }
}
