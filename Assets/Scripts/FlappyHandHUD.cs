using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// จอเล็กๆ มุมขวาบนของหน้าจอ Flappy Bird แสดงภาพสดจากกล้อง (ให้ผู้เล่นเช็คได้ว่าตัวเองอยู่ในเฟรมไหม)
/// พร้อมข้อความสถานะท่ามือปัจจุบัน (กำมือ/แบมือ/อื่นๆ/ไม่พบมือ) — ดึงข้อมูลจาก HandTrackingHub
/// ตรงๆ ตัวเดียวกับที่ใช้ควบคุมการกระโดดจริง เพื่อให้ผู้เล่นเช็คสถานะตรงกับที่เกมใช้งานจริง
///
/// สคริปต์นี้สร้าง Canvas/UI ของตัวเองทั้งหมดตอนรัน ไม่ต้องตั้งค่าอะไรในซีนล่วงหน้า
/// แค่มี GameObject ที่ติดสคริปต์นี้อยู่ในซีนก็พอ (ไม่กระทบ UI/Canvas เดิมของเกมเลย)
/// </summary>
public class FlappyHandHUD : MonoBehaviour
{
    [Header("ขนาด/ตำแหน่งจอพรีวิว")]
    public Vector2 previewSize = new Vector2(320, 240);
    public Vector2 margin = new Vector2(16, 16);

    WebcamStreamReceiver streamReceiver;
    RawImage previewImage;
    TMP_Text statusText;

    void Awake()
    {
        streamReceiver = GetComponent<WebcamStreamReceiver>();
        if (streamReceiver == null)
            streamReceiver = gameObject.AddComponent<WebcamStreamReceiver>();

        BuildUI();
    }

    void BuildUI()
    {
        var canvasGO = new GameObject("HandHUD_Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGO.transform.SetParent(transform, false);
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000; // ให้ลอยอยู่บนสุดเสมอ ไม่โดน UI อื่นของเกมบัง

        // จอพรีวิวภาพสด มุมขวาบน
        var imageGO = new GameObject("PreviewImage", typeof(RawImage));
        imageGO.transform.SetParent(canvasGO.transform, false);
        previewImage = imageGO.GetComponent<RawImage>();
        var imageRect = previewImage.rectTransform;
        imageRect.anchorMin = new Vector2(1, 1);
        imageRect.anchorMax = new Vector2(1, 1);
        imageRect.pivot = new Vector2(1, 1);
        imageRect.sizeDelta = previewSize;
        imageRect.anchoredPosition = new Vector2(-margin.x, -margin.y);

        // ข้อความสถานะ อยู่ใต้จอพรีวิว
        var textGO = new GameObject("StatusText", typeof(TextMeshProUGUI));
        textGO.transform.SetParent(canvasGO.transform, false);
        statusText = textGO.GetComponent<TextMeshProUGUI>();
        statusText.alignment = TextAlignmentOptions.TopRight;
        statusText.fontSize = 32;
        statusText.fontStyle = FontStyles.Bold;
        statusText.color = Color.white;
        statusText.text = "Hand: -";
        var textRect = statusText.rectTransform;
        textRect.anchorMin = new Vector2(1, 1);
        textRect.anchorMax = new Vector2(1, 1);
        textRect.pivot = new Vector2(1, 1);
        textRect.sizeDelta = new Vector2(previewSize.x, 40);
        textRect.anchoredPosition = new Vector2(-margin.x, -margin.y - previewSize.y - 4);
    }

    void Update()
    {
        if (streamReceiver != null && streamReceiver.HasFrame && previewImage.texture == null)
        {
            previewImage.texture = streamReceiver.CurrentFrame;
        }

        var hub = HandTrackingHub.Instance;
        if (hub != null && statusText != null)
        {
            statusText.text = "Hand: " + GestureLabel(hub.CurrentGesture);
        }
    }

    // ใช้ข้อความภาษาอังกฤษเพราะฟอนต์ default ของ TextMeshPro (LiberationSans SDF) ไม่มีตัวอักษรไทย
    // ถ้าใส่ข้อความไทยไปตรงๆ จะเห็นเป็นกล่องเหลี่ยมๆ (missing glyph) แทน
    static string GestureLabel(string gesture)
    {
        switch (gesture)
        {
            case "fist": return "FIST";
            case "open": return "OPEN";
            case "other": return "OTHER";
            default: return "NONE";
        }
    }
}
