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
        if (titleText != null) titleText.text = FriendLanguageUI.T("ROUND COMPLETE", "จบรอบ");
        if (reasonText != null) reasonText.text = FriendLanguageUI.T("TIME UP", "หมดเวลา");
        if (playerText != null) playerText.text = FriendLanguageUI.T("PLAYER: ", "ผู้เล่น: ") + (string.IsNullOrWhiteSpace(playerName) ? "GUEST" : playerName.Trim().ToUpper());
        if (scoreText != null) scoreText.text = FriendLanguageUI.T("SCORE: ", "คะแนน: ") + score;
        if (promptText != null) promptText.text = FriendLanguageUI.T("PRESS ANY BUTTON TO CONTINUE", "กดปุ่มใดก็ได้");
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
