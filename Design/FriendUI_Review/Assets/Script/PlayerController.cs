using UnityEngine;

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

        // ตอนนี้ใช้คีย์บอร์ดแทนค่าจาก ESP32/Sensor ไปก่อน
        // ภายหลังแค่เปลี่ยนตรงนี้เป็นค่าที่ได้จาก WebSocket แทนได้เลย
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) input = -1f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) input = 1f;

        Vector3 pos = transform.position;
        pos.x += input * moveSpeed * Time.deltaTime;
        pos.x = Mathf.Clamp(pos.x, minX, maxX);
        transform.position = pos;
    }
}
