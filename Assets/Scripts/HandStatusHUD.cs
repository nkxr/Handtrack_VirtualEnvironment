using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// ตัวแสดงสถานะการตรวจจับมือแบบเรียลไทม์ มุมบนซ้ายของจอ (ซีน jigsaw/tah) — สร้าง Canvas/Text/จุดสี
/// ทั้งหมดด้วยโค้ดตอน Awake() (แบบเดียวกับ FlappyHandHUD/HomeMenu) ไม่ต้องผูก UI ในไฟล์ .unity เลย
///
/// เหตุผลที่ทำ: ผู้ใช้แจ้งว่าเวลากำมือค้างไว้เพื่อลากครอบภาพ บางทีการตรวจจับมือหลุดไปเสี้ยววินาที
/// (มือหลุดโฟกัสกล้อง/แสงกระพริบ/หมุนมือเร็ว) ทำให้ไม่รู้ว่าตอนนี้ระบบยังจับมืออยู่ไหม หรือกำลังจะ
/// ปล่อยเมาส์เพราะนับว่าเป็นการแบมือจริง ตัวนี้จึงโชว์ทั้งสถานะ "เจอมือไหม" และ "ท่ามือปัจจุบันคืออะไร"
/// ให้เห็นสดๆ ระหว่างเล่น
/// </summary>
public class HandStatusHUD : MonoBehaviour
{
    [Header("ตำแหน่งกล่องสถานะ (มุมบนซ้าย)")]
    public Vector2 anchoredPosition = new Vector2(16f, -16f);
    public Vector2 boxSize = new Vector2(260f, 64f);

    TextMeshProUGUI statusText;
    Image dotImage;

    static readonly Color ColorNoHand = new Color(0.75f, 0.2f, 0.2f);     // แดง — ไม่เจอมือเลย
    static readonly Color ColorFist = new Color(1f, 0.75f, 0.15f);       // เหลืองส้ม — กำมือ (กำลังลาก/ค้างคลิก)
    static readonly Color ColorOpen = new Color(0.25f, 0.8f, 0.35f);     // เขียว — แบมือ/พร้อมใช้งาน
    static readonly Color ColorOther = new Color(0.4f, 0.65f, 0.95f);    // ฟ้า — ท่าอื่นๆ ที่ระบบจับได้

    void Awake()
    {
        BuildUI();
    }

    void BuildUI()
    {
        GameObject canvasGO = new GameObject("HandStatusHUD_Canvas");
        canvasGO.transform.SetParent(transform, false);
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500; // ต้องอยู่บนสุดเสมอ ไม่ให้อะไรมาบังตัวบอกสถานะ

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        // กล่องพื้นหลังโปร่งแสง มุมบนซ้าย
        GameObject panelGO = new GameObject("Panel");
        panelGO.transform.SetParent(canvasGO.transform, false);
        RectTransform panelRT = panelGO.AddComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(0f, 1f);
        panelRT.anchorMax = new Vector2(0f, 1f);
        panelRT.pivot = new Vector2(0f, 1f);
        panelRT.anchoredPosition = anchoredPosition;
        panelRT.sizeDelta = boxSize;

        Image panelImage = panelGO.AddComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.45f);

        // จุดสีบอกสถานะ (ซ้ายในกล่อง)
        GameObject dotGO = new GameObject("StatusDot");
        dotGO.transform.SetParent(panelGO.transform, false);
        RectTransform dotRT = dotGO.AddComponent<RectTransform>();
        dotRT.anchorMin = new Vector2(0f, 0.5f);
        dotRT.anchorMax = new Vector2(0f, 0.5f);
        dotRT.pivot = new Vector2(0f, 0.5f);
        dotRT.anchoredPosition = new Vector2(14f, 0f);
        dotRT.sizeDelta = new Vector2(22f, 22f);
        dotImage = dotGO.AddComponent<Image>();
        dotImage.color = ColorNoHand;

        // ข้อความสถานะ (ขวาของจุดสี)
        GameObject textGO = new GameObject("StatusText");
        textGO.transform.SetParent(panelGO.transform, false);
        RectTransform textRT = textGO.AddComponent<RectTransform>();
        textRT.anchorMin = new Vector2(0f, 0f);
        textRT.anchorMax = new Vector2(1f, 1f);
        textRT.pivot = new Vector2(0f, 0.5f);
        textRT.offsetMin = new Vector2(48f, 4f);
        textRT.offsetMax = new Vector2(-8f, -4f);

        statusText = textGO.AddComponent<TextMeshProUGUI>();
        statusText.text = "กำลังหามือ...";
        statusText.fontSize = 20f;
        statusText.color = Color.white;
        statusText.alignment = TextAlignmentOptions.MidlineLeft;
        statusText.enableWordWrapping = true;
    }

    void Update()
    {
        var hub = HandTrackingHub.Instance;
        if (hub == null || !hub.HandDetected)
        {
            SetState(ColorNoHand, "ไม่พบมือ");
            return;
        }

        switch (hub.CurrentGesture)
        {
            case "fist":
                SetState(ColorFist, "กำมือ (กำลังลาก/คลิกค้าง)");
                break;
            case "open":
                SetState(ColorOpen, "แบมือ (พร้อมใช้งาน)");
                break;
            default:
                SetState(ColorOther, "เจอมือ (ท่า: " + hub.CurrentGesture + ")");
                break;
        }
    }

    void SetState(Color color, string label)
    {
        if (dotImage != null) dotImage.color = color;
        if (statusText != null) statusText.text = label;
    }
}
