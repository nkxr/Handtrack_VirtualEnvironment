using UnityEngine;
using UnityEngine.UI;

public class WebCamManager : MonoBehaviour
{
    [Header("UI Elements")]
    public RawImage camDisplay;          // WebCamScreen
    public RectTransform cropFrame;      // CropFrame
    public RawImage croppedDisplay;      // CroppedDisplay (ปิดไปเลยไม่ได้ใช้)

    [Header("Scripts & Settings")]
    public PuzzleGenerator puzzleGenerator;

    // เดิมใช้ WebCamTexture เปิดกล้องตรงๆ แต่เปิดพร้อมกับ Python (ที่ทำ hand tracking
    // อยู่บนกล้องตัวเดียวกัน) ไม่ได้ เลยเปลี่ยนมารับภาพผ่าน TCP จาก main_jigsaw.py แทน
    private WebcamStreamReceiver streamReceiver;

    // เพิ่มตัวแปรดักการแคปเจอร์ไว้ตรงนี้ (ระดับ Class)
    private bool isCaptured = false;

    void Awake()
    {
        streamReceiver = GetComponent<WebcamStreamReceiver>();
        if (streamReceiver == null)
            streamReceiver = gameObject.AddComponent<WebcamStreamReceiver>();
    }

    void Start()
    {
        if (camDisplay != null)
        {
            camDisplay.texture = streamReceiver.CurrentFrame;
        }

        if (croppedDisplay != null) croppedDisplay.gameObject.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            TakeSnapshot();
        }
    }

    public void TakeSnapshot()
    {
        // 1. ดักไว้เลยว่าถ้าเคยแคปภาพไปแล้ว (isCaptured == true) ให้เด้งออกทันที ไม่ทำโค้ดด้านล่างต่อ
        if (isCaptured) return;

        // 2. เช็คว่ามีภาพจากฝั่ง Python เข้ามาแล้วหรือยัง
        if (streamReceiver == null || !streamReceiver.HasFrame) return;

        Texture2D liveFrame = streamReceiver.CurrentFrame;

        // 3. แคปภาพเต็มจอจากภาพที่รับมา ณ วินาทีนั้นไว้ทำ Background
        Texture2D fullPhoto = new Texture2D(liveFrame.width, liveFrame.height);
        fullPhoto.SetPixels(liveFrame.GetPixels());
        fullPhoto.Apply();

        // 4. บันทึกสถานะว่า "แคปภาพแล้ว" เพื่อที่พอกด Spacebar ครั้งหน้า ระบบจะเด้งออกที่บรรทัดบนสุดทันที
        isCaptured = true;

        // ซ่อนเฉพาะกรอบเล็งเขียวๆ ออกไป
        if (cropFrame != null) cropFrame.gameObject.SetActive(false);

        // 5. คำนวณพิกัดเพื่อ Crop ภาพเฉพาะตรงกลางกรอบ CropFrame
        RectTransform camRect = camDisplay.GetComponent<RectTransform>();

        float scaleX = (float)liveFrame.width / camRect.rect.width;
        float scaleY = (float)liveFrame.height / camRect.rect.height;

        int cropWidth = Mathf.RoundToInt(cropFrame.rect.width * scaleX);
        int cropHeight = Mathf.RoundToInt(cropFrame.rect.height * scaleY);

        int startX = Mathf.RoundToInt((liveFrame.width - cropWidth) / 2f);
        int startY = Mathf.RoundToInt((liveFrame.height - cropHeight) / 2f);

        // ดึงพิกเซลเดิมออกมาก่อน
        Color[] originalPixels = fullPhoto.GetPixels(startX, startY, cropWidth, cropHeight);

        // --- ส่วนที่เพิ่มเข้ามา: สลับพิกเซลซ้าย-ขวา (Mirror) ---
        Color[] flippedPixels = new Color[originalPixels.Length];
        for (int y = 0; y < cropHeight; y++)
        {
            for (int x = 0; x < cropWidth; x++)
            {
                // คำนวณ Index เพื่อสลับจากขวามาซ้าย
                int originalIndex = y * cropWidth + x;
                int flippedIndex = y * cropWidth + (cropWidth - 1 - x);
                flippedPixels[flippedIndex] = originalPixels[originalIndex];
            }
        }

        // นำพิกเซลที่สลับแล้วไปสร้างรูปภาพใหม่
        Texture2D croppedPhoto = new Texture2D(cropWidth, cropHeight);
        croppedPhoto.SetPixels(flippedPixels);
        croppedPhoto.Apply();

        if (puzzleGenerator != null)
        {
            puzzleGenerator.CreatePuzzle(croppedPhoto);
        }
    }
}
