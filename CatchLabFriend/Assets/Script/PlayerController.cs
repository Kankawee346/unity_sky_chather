using UnityEngine;
using UnityEngine.InputSystem;

// แนบไว้บน GameObject ตัวละครผู้เล่น
// ต้องมี Rigidbody2D (Body Type = Kinematic) และ Collider2D (ติ๊ก Is Trigger) ด้วย
// และตั้ง Tag ของ GameObject นี้เป็น "Player"
public class PlayerController : MonoBehaviour
{
    [Header("ความเร็วในการเลื่อน")]
    public float moveSpeed = 8f;

    [Header("ขอบเขตซ้าย-ขวาที่เลื่อนได้ (กันตัวละครหลุดจอ)")]
    public float minX = -4f;
    public float maxX = 4f;

    void Update()
    {
        if (GameManager.Instance == null || !GameManager.Instance.IsRunning) return;

        float input = 0f;

        var keyboard = Keyboard.current;
        bool keyboardLeft = keyboard != null && (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed);
        bool keyboardRight = keyboard != null && (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed);
        input = keyboardLeft == keyboardRight ? 0f : keyboardLeft ? -1f : 1f;
        if (input == 0f) input = FriendControllerReceiver.Direction;

        Vector3 pos = transform.position;
        pos.x += input * moveSpeed * Time.deltaTime;
        float leftLimit = minX;
        float rightLimit = maxX;
        if (Camera.main != null && Camera.main.orthographic)
        {
            var sprite = GetComponentInChildren<SpriteRenderer>();
            float halfWidth = sprite != null ? sprite.bounds.extents.x : 0.3f;
            leftLimit = Mathf.Max(leftLimit, Camera.main.ViewportToWorldPoint(new Vector3(0f, 0.5f)).x + halfWidth);
            rightLimit = Mathf.Min(rightLimit, Camera.main.ViewportToWorldPoint(new Vector3(1f, 0.5f)).x - halfWidth);
        }
        pos.x = leftLimit > rightLimit ? (leftLimit + rightLimit) * 0.5f : Mathf.Clamp(pos.x, leftLimit, rightLimit);
        transform.position = pos;
    }
}
