using UnityEngine;

// Attach to every falling prefab (reward, fuel, obstacle). Needs a Collider2D with Is Trigger on.
public class FallingObject : MonoBehaviour
{
    public enum ItemType { Reward, Fuel, Obstacle }

    [Header("What is this item?")]
    public ItemType itemType = ItemType.Reward;

    [Header("Set by SpawnManager at spawn time")]
    public float fallSpeed = 2f;

    [Header("Reward: score added on catch")]
    public int scoreValue = 10;

    [Header("Fuel: fuel added on catch (also gives score below)")]
    public float fuelAmount = 20f;
    public int fuelScore = 0;

    [Header("Obstacle: fuel removed when the plane is hit")]
    public float fuelDamage = 15f;

    [Header("Below this Y the item counts as passed")]
    public float missY = -6f;

    [Tooltip("Reward / Fuel only: passing this item breaks the combo")]
    public bool breaksComboWhenMissed = true;

    [Header("Sounds (optional)")]
    public AudioClip catchSound;   // reward / fuel pickup, or the hit sound for obstacles
    public AudioClip missSound;

    [Header("Spawning")]
    [Tooltip("Higher = spawns more often compared to other prefabs")]
    public int spawnWeight = 1;

    [Tooltip("Multiplier applied to spawnWeight on Easy/Normal/Hard (e.g. lower on Hard to make obstacles less spammy)")]
    public float easySpawnWeightMultiplier = 1f;
    public float normalSpawnWeightMultiplier = 1f;
    public float hardSpawnWeightMultiplier = 1f;

    [Tooltip("Tick for items that should always fall far away from other items")]
    public bool needsExtraSpacing = false;

    [Tooltip("Optional group name. While another item with the SAME tag is active, this item will not spawn next to it (e.g. use \"Redcoin\" on the rare coin).")]
    public string spawnTag = "";

    [Header("Difficulty scaling (e.g. obstacles)")]
    [Tooltip("Scale this object based on GameManager complexity (Easy/Normal/Hard) when spawned")]
    public bool scaleWithDifficulty = false;
    public float easyScaleMultiplier = 0.85f;
    public float normalScaleMultiplier = 1f;
    public float hardScaleMultiplier = 1.35f;

    [Header("Fall speed override")]
    [Tooltip("If true, uses customFallSpeed instead of the speed assigned by SpawnManager")]
    public bool overrideFallSpeed = false;
    public float customFallSpeed = 2f;

    private Vector3 baseLocalScale;
    private bool baseScaleCaptured;

    // Tracked active instances (avoids scene searches in SpawnManager hot path)
    public static readonly System.Collections.Generic.List<FallingObject> Active = new System.Collections.Generic.List<FallingObject>(64);

    void OnEnable()
    {
        if (!Active.Contains(this)) Active.Add(this);
    }

    void OnDisable()
    {
        Active.Remove(this);
    }

    void Start()
    {
        if (!baseScaleCaptured)
        {
            baseLocalScale = transform.localScale;
            baseScaleCaptured = true;
        }
        ApplyDifficultyScale();
        if (overrideFallSpeed)
        {
            fallSpeed = Mathf.Max(0.01f, customFallSpeed);
        }
    }

    void ApplyDifficultyScale()
    {
        if (!scaleWithDifficulty || GameManager.Instance == null) return;
        if (!baseScaleCaptured)
        {
            baseLocalScale = transform.localScale;
            baseScaleCaptured = true;
        }
        float m = normalScaleMultiplier;
        switch (GameManager.Instance.complexity)
        {
            case 1: m = easyScaleMultiplier; break;
            case 3: m = hardScaleMultiplier; break;
        }
        transform.localScale = baseLocalScale * Mathf.Max(0.01f, m);
    }

    void Update()
    {
        transform.position += Vector3.down * fallSpeed * Time.deltaTime;

        if (transform.position.y <= missY)
        {
            // Dodging an obstacle is the goal, so it is never a miss
            if (itemType != ItemType.Obstacle)
            {
                if (missSound != null) AudioSource.PlayClipAtPoint(missSound, transform.position);
                if (breaksComboWhenMissed) GameManager.Instance.RegisterMiss();
            }
            Destroy(gameObject);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player")) Collect();
    }

    void Collect()
    {
        if (catchSound != null) AudioSource.PlayClipAtPoint(catchSound, transform.position);

        switch (itemType)
        {
            case ItemType.Reward:
                GameManager.Instance.RegisterCatch(scoreValue);
                break;
            case ItemType.Fuel:
                GameManager.Instance.AddFuel(fuelAmount);
                if (fuelScore != 0) GameManager.Instance.AddScore(fuelScore);
                break;
            case ItemType.Obstacle:
                GameManager.Instance.RegisterHit(fuelDamage);
                break;
        }
        Destroy(gameObject);
    }
}