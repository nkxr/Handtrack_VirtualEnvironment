using UnityEngine;
using UnityEngine.EventSystems;

// เพิ่ม IPointerUpHandler เข้ามาต่อท้าย
public class CropAreaController : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public RectTransform cropFrame;
    public WebCamManager webCamManager; // เพิ่มตัวแปรเพื่อใช้เรียกคำสั่งถ่ายรูป
    private Vector2 startPos;

    public void OnPointerDown(PointerEventData eventData)
    {
        cropFrame.gameObject.SetActive(true);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            GetComponent<RectTransform>(), eventData.position, eventData.pressEventCamera, out startPos);

        cropFrame.localPosition = startPos;
        cropFrame.sizeDelta = Vector2.zero;
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 currentPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            GetComponent<RectTransform>(), eventData.position, eventData.pressEventCamera, out currentPos);

        Vector2 size = new Vector2(Mathf.Abs(currentPos.x - startPos.x), Mathf.Abs(currentPos.y - startPos.y));
        Vector2 center = (startPos + currentPos) / 2f;

        cropFrame.sizeDelta = size;
        cropFrame.localPosition = center;
    }

    // ฟังก์ชันใหม่! ทำงานทันทีเมื่อ "ปล่อยคลิกเมาส์"
    public void OnPointerUp(PointerEventData eventData)
    {
        // สั่งให้ WebCamManager ทำการถ่ายรูปทันทีที่วาดกรอบเสร็จ
        if (webCamManager != null)
        {
            webCamManager.TakeSnapshot();
        }
    }
}