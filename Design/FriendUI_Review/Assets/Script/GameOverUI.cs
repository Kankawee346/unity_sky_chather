using TMPro;
using UnityEngine;

public class GameOverUI : MonoBehaviour
{
    [Header("Refs")]
    public TMP_Text titleText;
    public TMP_Text reasonText;
    public TMP_Text playerText;
    public TMP_Text scoreText;
    public TMP_Text promptText;

    [Header("Sound")]
    public AudioClip gameOverSound;
    [Range(0f, 1f)] public float gameOverVolume = 1f;

    private AudioSource audioSource;

    public void Show(string playerName, int score)
    {
        if (titleText != null) titleText.text = "GAME OVER";
        if (reasonText != null) reasonText.text = "OUT OF FUEL";
        if (playerText != null) playerText.text = "PLAYER: " + (string.IsNullOrWhiteSpace(playerName) ? "GUEST" : playerName.Trim().ToUpper());
        if (scoreText != null) scoreText.text = "SCORE: " + score;
        if (promptText != null) promptText.text = "PRESS ANY BUTTON TO CONTINUE";
        gameObject.SetActive(true);
        PlayGameOverSound();
    }

    void PlayGameOverSound()
    {
        if (gameOverSound == null) return;
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f; // 2D sound
        }
        audioSource.PlayOneShot(gameOverSound, gameOverVolume);
    }
}
