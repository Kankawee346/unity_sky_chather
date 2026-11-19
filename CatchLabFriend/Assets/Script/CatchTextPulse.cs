using UnityEngine;
using TMPro;

// Restarts from the resting pose, so fast consecutive catches never accumulate scale or rotation.
public class CatchTextPulse : MonoBehaviour
{
    Vector3 restScale;
    Quaternion restRotation;
    Color restColor;
    TMP_Text label;
    float remaining, strength;
    void Awake()
    {
        label = GetComponent<TMP_Text>();
        restScale = transform.localScale;
        restRotation = transform.localRotation;
        restColor = label.color;
    }
    public static void Play(TMP_Text target, float amount)
    {
        if (target == null) return;
        var pulse = target.GetComponent<CatchTextPulse>();
        if (pulse == null) pulse = target.gameObject.AddComponent<CatchTextPulse>();
        pulse.remaining = .45f;
        pulse.strength = amount;
    }
    void Update()
    {
        if (remaining <= 0) return;
        remaining = Mathf.Max(0, remaining - Time.deltaTime);
        float t = remaining / .45f;
        transform.localScale = restScale * (1 + strength * Mathf.Sin(t * Mathf.PI));
        transform.localRotation = restRotation * Quaternion.Euler(0, 0, Mathf.Sin(t * 32) * t * strength * 12);
        label.color = Color.Lerp(restColor, new Color(1f, .85f, .2f), t);
        if (remaining == 0) ResetPose();
    }
    void OnDisable() { remaining = 0; ResetPose(); }
    void ResetPose()
    {
        transform.localScale = restScale;
        transform.localRotation = restRotation;
        if (label != null) label.color = restColor;
    }
}
