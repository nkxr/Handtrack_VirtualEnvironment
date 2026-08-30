using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class PlayerController : MonoBehaviour
{
    private Rigidbody2D rb;

    [SerializeField] private float jumpForce = 1f;
    [SerializeField] private Vector3 startPosition = new Vector3(-5f, 0f, 0f);

    [Header("Hand Tracking (เพิ่มเข้ามาเป็นทางเลือกเสริม ไม่ได้แก้/แทนที่ปุ่ม Space เดิม)")]
    [Tooltip("ถ้าติ๊กไว้ กำมือ (fist) จากกล้องจะสั่งให้นกลอยขึ้นค้างไว้ตลอดที่ยังกำมืออยู่ (เหมือนกดค้าง ไม่ใช่แค่กระตุกทีเดียว)")]
    [SerializeField] private bool enableHandControl = true;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Jump();
        }

        // ถืออยู่ในสถานะกำมือเมื่อไหร่ ก็สั่งลอยขึ้นซ้ำทุกเฟรมตอนนั้นเลย (เล่นง่ายกว่าเดิม
        // ไม่ต้องกำ-ปล่อย-กำใหม่ทีละครั้ง แค่กำค้างไว้นกก็ลอยขึ้นค้างไปเรื่อยๆ)
        if (enableHandControl && HandTrackingHub.Instance != null
            && HandTrackingHub.Instance.CurrentGesture == "fist")
        {
            Jump();
        }
    }

    private void Jump()
    {
        rb.velocity = new Vector2(rb.velocity.x, jumpForce);
    }

    public void ResetState()
    {
        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.rotation = 0f;
        transform.SetPositionAndRotation(startPosition, Quaternion.identity);
        rb.WakeUp();
    }
}
