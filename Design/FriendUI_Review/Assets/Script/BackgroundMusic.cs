using UnityEngine;

// Loops the background music during the menu and gameplay.
// When the round ends (out of fuel) the music stops so the game over sound
// effect is heard on its own, then it starts again when a new round begins.
public class BackgroundMusic : MonoBehaviour
{
    [Header("Music")]
    public AudioClip music;
    [Range(0f, 1f)] public float volume = 0.45f;
    [Tooltip("Start the music as soon as the scene loads (start menu)")]
    public bool playOnStart = true;

    private AudioSource source;

    void Awake()
    {
        source = GetComponent<AudioSource>();
        if (source == null) source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = true;
        source.spatialBlend = 0f; // 2D music
        source.volume = volume;
        source.clip = music;
    }

    void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameOver += StopMusic;
            GameManager.Instance.OnSessionStarted += StartMusic;
        }
        if (playOnStart) StartMusic();
    }

    void OnDestroy()
    {
        if (GameManager.Instance == null) return;
        GameManager.Instance.OnGameOver -= StopMusic;
        GameManager.Instance.OnSessionStarted -= StartMusic;
    }

    public void StartMusic()
    {
        if (source == null || source.clip == null) return;
        source.volume = volume;
        if (!source.isPlaying) source.Play();
    }

    public void StopMusic()
    {
        if (source != null) source.Stop();
    }
}
