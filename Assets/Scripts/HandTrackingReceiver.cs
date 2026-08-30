using UnityEngine;

/// <summary>
/// รับข้อมูลตำแหน่งจุดข้อมือ (21 landmarks) จาก HandTrackingHub (ตัวกลางที่รับ UDP จาก Python จริงๆ)
/// แล้วเอาไปขยับตำแหน่งลูกบอล (marker) 21 ลูกในซีน เพื่อแสดงผล/ทดสอบว่าข้อมูลมาถึงจริง
///
/// (เดิมสคริปต์นี้เปิด UDP socket เอง แต่ย้ายไปให้ HandTrackingHub เป็นเจ้าของ socket แทน
/// เพื่อให้ทุกซีนใช้ socket ตัวเดียวกันได้ ไม่ชนพอร์ตกันตอนสลับซีน/build เป็นโปรแกรมจริง)
///
/// วิธีใช้: สร้าง Empty GameObject ในซีน แล้ว Add Component สคริปต์นี้เข้าไป
/// รันซีนแล้วเปิดฝั่ง Python (Main.py) ทิ้งไว้ ถ้าเชื่อมสำเร็จจะเห็นลูกบอล 21 ลูกขยับตามมือ
/// </summary>
public class HandTrackingReceiver : MonoBehaviour
{
    [Header("Visualization")]
    [Tooltip("ลูกบอลแสดงจุด landmark ถ้าไม่ใส่ไว้ ระบบจะสร้าง Sphere ให้อัตโนมัติ 21 ลูก")]
    public GameObject markerPrefab;
    [Tooltip("สเกลแปลงจากพิกเซลภาพกล้อง (ประมาณ 0-1280, 0-720) เป็นหน่วยของ Unity")]
    public float positionScale = 0.01f;
    [Tooltip("จุดกึ่งกลางเฟรมกล้อง (กว้าง, สูง) ใช้เลื่อน landmark ให้อยู่รอบจุด origin ของวัตถุนี้")]
    public Vector2 frameCenter = new Vector2(640f, 360f);
    [Tooltip("กลับแกน X ให้เหมือนกระจก (ยกมือซ้ายจริง แล้วเห็นวัตถุขยับไปทางซ้ายของจอด้วย)")]
    public bool mirrorX = true;
    [Tooltip("ถ้าไม่มีข้อมูลใหม่เข้ามานานเกินนี้ (วินาที) จะซ่อน marker ทั้งหมด")]
    public float dataTimeout = 0.5f;

    public const int LandmarkCount = 21;

    Transform[] markers;
    float lastDataTime;

    void Start()
    {
        CreateMarkersIfNeeded();
    }

    void CreateMarkersIfNeeded()
    {
        markers = new Transform[LandmarkCount];
        for (int i = 0; i < LandmarkCount; i++)
        {
            GameObject go;
            if (markerPrefab != null)
            {
                go = Instantiate(markerPrefab, transform);
            }
            else
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                go.transform.SetParent(transform);
                go.transform.localScale = Vector3.one * 0.05f;
            }
            go.name = "Landmark_" + i;
            go.SetActive(false);
            markers[i] = go.transform;
        }
    }

    void Update()
    {
        var hub = HandTrackingHub.Instance;
        if (hub == null) return;

        if (hub.HandDetected && hub.Landmarks != null && hub.Landmarks.Length >= LandmarkCount * 3)
        {
            lastDataTime = Time.time;
            ApplyLandmarks(hub.Landmarks);
            SetMarkersActive(true);
        }

        if (Time.time - lastDataTime > dataTimeout)
        {
            SetMarkersActive(false);
        }
    }

    void ApplyLandmarks(float[] data)
    {
        for (int i = 0; i < LandmarkCount; i++)
        {
            float x = data[i * 3 + 0];
            float y = data[i * 3 + 1];
            float z = data[i * 3 + 2];

            float signedX = mirrorX ? (frameCenter.x - x) : (x - frameCenter.x);

            Vector3 localPos = new Vector3(
                signedX * positionScale,
                (y - frameCenter.y) * positionScale,
                z * positionScale
            );
            markers[i].localPosition = localPos;
        }
    }

    void SetMarkersActive(bool active)
    {
        if (markers == null) return;
        foreach (var m in markers)
        {
            if (m != null) m.gameObject.SetActive(active);
        }
    }
}
