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

    private WebCamTexture webCamTexture;

    // เพิ่มตัวแปรดักการแคปเจอร์ไว้ตรงนี้ (ระดับ Class)
    private bool isCaptured = false;

    void Start()
    {
        webCamTexture = new WebCamTexture();
        if (camDisplay != null)
        {
            camDisplay.texture = webCamTexture;
        }
        webCamTexture.Play();

        if (croppedDisplay != null) croppedDisplay.gameObject.SetActive(false);
    }

    void OnDestroy()
    {
        if (webCamTexture != null)
        {
            webCamTexture.Stop(); // สั่งหยุดส่งสัญญาณกล้อง
            Debug.Log("กล้องถูกปิดการใช้งานเพื่อเตรียมเริ่มใหม่");
        }
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

        // 2. เช็คว่ากล้องพร้อมทำงานไหม
        if (webCamTexture == null || !webCamTexture.isPlaying) return;

        // 3. แคปภาพเต็มจอจากกล้อง ณ วินาทีนั้นไว้ทำ Background
        Texture2D fullPhoto = new Texture2D(webCamTexture.width, webCamTexture.height);
        fullPhoto.SetPixels(webCamTexture.GetPixels());
        fullPhoto.Apply();

        // 4. บันทึกสถานะว่า "แคปภาพแล้ว" เพื่อที่พอกด Spacebar ครั้งหน้า ระบบจะเด้งออกที่บรรทัดบนสุดทันที
        isCaptured = true;

        // ==========================================
        // 1. สั่งหยุดการทำงานของสคริปต์ CropAreaController (เพื่อไม่ให้มันแอบเปิดกรอบเขียวกลับมาอีก)
        if (camDisplay != null)
        {
            MonoBehaviour cropController = camDisplay.GetComponent("CropAreaController") as MonoBehaviour;
            if (cropController != null) cropController.enabled = false;
        }

        // 2. ซ่อนเฉพาะกรอบเล็งเขียวๆ ออกไป (คราวนี้ภาพกล้องยังอยู่ และกรอบหายชัวร์ครับ)
        if (cropFrame != null)
        {
            cropFrame.gameObject.SetActive(false);
        }
        // ==========================================

        // 5. คำนวณพิกัดเพื่อ Crop ภาพเฉพาะตรงกลางกรอบ CropFrame
        RectTransform camRect = camDisplay.GetComponent<RectTransform>();

        float scaleX = (float)webCamTexture.width / camRect.rect.width;
        float scaleY = (float)webCamTexture.height / camRect.rect.height;

        int cropWidth = Mathf.RoundToInt(cropFrame.rect.width * scaleX);
        int cropHeight = Mathf.RoundToInt(cropFrame.rect.height * scaleY);

        int startX = Mathf.RoundToInt((webCamTexture.width - cropWidth) / 2f);
        int startY = Mathf.RoundToInt((webCamTexture.height - cropHeight) / 2f);

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