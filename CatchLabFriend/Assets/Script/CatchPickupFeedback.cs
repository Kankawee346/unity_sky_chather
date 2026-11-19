using UnityEngine;
using TMPro;

public class CatchPickupFeedback : MonoBehaviour
{
    static Sprite pixel;
    SpriteRenderer[] sparks;
    Vector3[] directions;
    TMP_Text popup;
    Vector2 popupOrigin;
    Color tint;
    float age;

    public static void Show(Vector3 position, int points, float fuel, Material material)
    {
        var effect = new GameObject("Pickup feedback").AddComponent<CatchPickupFeedback>();
        effect.transform.position = position;
        effect.tint = fuel > 0 ? new Color(.4f, 1f, .55f) : points >= 10 ? new Color(1f, .3f, .35f) : new Color(1f, .85f, .2f);
        if (pixel == null)
        {
            var texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, Color.white); texture.Apply();
            texture.filterMode = FilterMode.Point;
            pixel = Sprite.Create(texture, new Rect(0, 0, 1, 1), Vector2.one * .5f, 1);
        }
        effect.sparks = new SpriteRenderer[10];
        effect.directions = new Vector3[10];
        for (int i = 0; i < 10; i++)
        {
            var spark = new GameObject("Pixel spark").AddComponent<SpriteRenderer>();
            spark.transform.SetParent(effect.transform, false);
            spark.sprite = pixel; spark.sharedMaterial = material; spark.sortingOrder = 30;
            spark.color = effect.tint;
            spark.transform.localScale = Vector3.one * .1f;
            float angle = i * Mathf.PI * 2 / 10;
            effect.directions[i] = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0);
            effect.sparks[i] = spark;
        }
        var ui = Object.FindFirstObjectByType<UIManager>();
        if (ui == null || ui.scoreText == null || Camera.main == null) return;
        Canvas canvas = ui.scoreText.canvas;
        if (canvas == null) return;
        var obj = new GameObject("Pickup amount", typeof(RectTransform));
        obj.transform.SetParent(canvas.transform, false);
        var label = obj.AddComponent<TextMeshProUGUI>();
        label.font = ui.scoreText.font;
        label.fontSize = 44;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        label.color = effect.tint;
        label.outlineWidth = .2f;
        label.outlineColor = new Color32(12, 22, 40, 255);
        label.text = fuel > 0 ? FriendLanguageUI.T("FUEL +", "พลัง +") + fuel.ToString("0") + (points > 0 ? "  +" + points : "") : "+" + points;
        label.rectTransform.sizeDelta = new Vector2(300, 85);
        Vector2 screen = Camera.main.WorldToScreenPoint(position + Vector3.up * .5f);
        RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)canvas.transform, screen,
            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera, out effect.popupOrigin);
        label.rectTransform.anchoredPosition = effect.popupOrigin;
        effect.popup = label;
    }
    void Update()
    {
        if (GameManager.Instance == null || !GameManager.Instance.IsRunning) { Destroy(gameObject); return; }
        age += Time.deltaTime;
        float t = Mathf.Clamp01(age / .8f);
        for (int i = 0; i < sparks.Length; i++)
        {
            sparks[i].transform.localPosition = directions[i] * (.1f + t * .85f);
            sparks[i].transform.localScale = Vector3.one * (.12f * (1 - t));
            sparks[i].color = new Color(tint.r, tint.g, tint.b, 1 - t);
        }
        if (popup != null)
        {
            popup.rectTransform.anchoredPosition = popupOrigin + Vector2.up * (t * 85);
            popup.transform.localScale = Vector3.one * (1 + .25f * Mathf.Sin(t * Mathf.PI));
            popup.alpha = 1 - Mathf.Clamp01((t - .55f) / .45f);
        }
        if (t >= 1) Destroy(gameObject);
    }
    void OnDestroy() { if (popup != null) Destroy(popup.gameObject); }
}
