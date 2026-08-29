using UnityEngine;

public class PuzzlePiece : MonoBehaviour
{
    public Vector3 correctPosition; // ตำแหน่งที่ถูกต้องของชิ้นนี้
    private Vector3 startPos;       // ตำแหน่งก่อนเริ่มลาก
    private Vector3 offset;
    private float zCoord;

    void OnMouseDown()

    {
        if (GameManager.instance != null) GameManager.instance.StartTimer();

        zCoord = Camera.main.WorldToScreenPoint(transform.position).z;

        // เมื่อคลิกเมาส์ค้างที่ชิ้นส่วน
        zCoord = Camera.main.WorldToScreenPoint(transform.position).z;
        offset = transform.position - GetMouseAsWorldPoint();
        startPos = transform.localPosition; // จำตำแหน่งเริ่มต้นก่อนลากไว้

        // ขยับชิ้นที่กำลังลากให้ลอยขึ้นมาข้างหน้าตัวอื่นนิดนึง จะได้ไม่จม
        transform.SetAsLastSibling();
        Vector3 tempPos = transform.localPosition;
        tempPos.z = -1f;
        transform.localPosition = tempPos;
    }

    void OnMouseDrag()
    {
        // อัปเดตตำแหน่งชิ้นส่วนตามเมาส์
        transform.position = GetMouseAsWorldPoint() + offset;
    }

    void OnMouseUp()
    {
        // เมื่อปล่อยเมาส์ ยิง Raycast เพื่อดูว่าไปวางทับชิ้นอื่นไหม
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit[] hits = Physics.RaycastAll(ray);

        bool swapped = false;

        foreach (RaycastHit hit in hits)
        {
            // ถ้าชนกับชิ้นส่วนอื่นที่ไม่ใช่ตัวเอง
            if (hit.collider.gameObject != this.gameObject && hit.collider.GetComponent<PuzzlePiece>() != null)
            {
                PuzzlePiece otherPiece = hit.collider.GetComponent<PuzzlePiece>();

                // สลับตำแหน่งกัน
                transform.localPosition = otherPiece.transform.localPosition;
                otherPiece.transform.localPosition = startPos;

                swapped = true;
                break;
            }
        }

        // ถ้าปล่อยกลางอากาศ ไม่ได้ทับชิ้นไหน ให้เด้งกลับที่เดิม
        if (!swapped)
        {
            transform.localPosition = startPos;
        }
        else
        {
            // จัด Z ให้อยู่ระนาบเดียวกัน (0) หลังจากสลับเสร็จ
            Vector3 finalPos = transform.localPosition;
            finalPos.z = 0;
            transform.localPosition = finalPos;
        }

        CheckWinCondition();
    }

    private Vector3 GetMouseAsWorldPoint()
    {
        Vector3 mousePoint = Input.mousePosition;
        mousePoint.z = zCoord;
        return Camera.main.ScreenToWorldPoint(mousePoint);
    }

    private void CheckWinCondition()
    {
        PuzzlePiece[] allPieces = transform.parent.GetComponentsInChildren<PuzzlePiece>();
        int correctCount = 0;

        foreach (var piece in allPieces)
        {
            // เปลี่ยนมาเช็คระยะห่างเฉพาะแกน X และ Y (Vector2) ป้องกันแกน Z รวน
            Vector2 currentPos = new Vector2(piece.transform.localPosition.x, piece.transform.localPosition.y);
            Vector2 targetPos = new Vector2(piece.correctPosition.x, piece.correctPosition.y);

            if (Vector2.Distance(currentPos, targetPos) < 0.1f)
            {
                correctCount++;
            }
        }

        // ปริ้นท์บอกใน Console ว่าตอนนี้ต่อถูกกี่ชิ้นแล้ว จะได้เช็คได้ง่ายๆ
        Debug.Log($"กำลังเช็คความถูกต้อง: ถูกแล้ว {correctCount} / {allPieces.Length} ชิ้น");

        if (correctCount == allPieces.Length)
        {
            Debug.Log("🎉 ครบทุกชิ้นแล้ว! สั่งเปิด Win Text!");
            if (GameManager.instance != null) GameManager.instance.TriggerWin();
        }
    }
}