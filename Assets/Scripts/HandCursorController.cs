using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using UnityEngine;

/// <summary>
/// ใช้ตำแหน่งปลายนิ้วชี้ (landmark 8 ตามลำดับของ MediaPipe Hands) แทนเมาส์จริงของเครื่อง
/// (Windows เท่านั้น — ใช้ user32.dll ผ่าน P/Invoke)
///
/// กำมือ (gesture == "fist") = กดปุ่มซ้ายค้างไว้ (เหมือนลากเมาส์ค้าง)
/// แบมือ/อื่นๆ/ไม่พบมือ = ปล่อยปุ่ม
///
/// ทำแบบนี้เพราะ CropAreaController.cs ใช้ระบบ UI Event ของ Unity ส่วน PuzzlePiece.cs ใช้
/// OnMouseDown/OnMouseDrag/OnMouseUp ที่อ่าน Input.mousePosition ตรงๆ ในโค้ด — Unity ไม่มีทาง
/// "หลอก" ค่า Input.mousePosition จากสคริปต์ภายนอกได้ วิธีเดียวที่ทำให้ทั้งสองระบบตอบสนองกับมือได้
/// โดยไม่ต้องแก้ไฟล์ทั้งสองไฟล์นั้นเลยแม้แต่บรรทัดเดียว คือสั่งขยับเมาส์จริงของ Windows แทน
///
/// หมายเหตุ: ขณะสคริปต์นี้ทำงาน มันจะขยับเคอร์เซอร์เมาส์จริงบนจอไปเรื่อยๆ ตามนิ้วชี้ ถ้าขยับเมาส์จริง
/// พร้อมกันไปด้วยจะแย่งตำแหน่งกัน (เป็นธรรมชาติของการให้มือแทนเมาส์ทั้งหมด)
/// </summary>
public class HandCursorController : MonoBehaviour
{
    [Header("การจับคู่พิกัด")]
    [Tooltip("ต้องตรงกับความละเอียดกล้องที่ตั้งไว้ฝั่ง Python (cap.set(3, ..), cap.set(4, ..) ใน Main.py)")]
    public int cameraFrameWidth = 1280;
    public int cameraFrameHeight = 720;
    [Tooltip("กลับแกน X หรือไม่ — ปิดไว้ (false) เป็นค่าเริ่มต้น เพื่อให้เคอร์เซอร์ตรงกับตำแหน่งตัวเองในภาพสด" +
              "ที่ไม่ได้กลับด้าน (ชี้ตรงไหนในภาพ เคอร์เซอร์ก็ไปอยู่ตรงนั้น) ลองสลับดูได้ถ้าเล่นแล้วรู้สึกกลับด้าน")]
    public bool mirrorX = true;

    [Header("จุดที่ใช้เป็นตัวชี้")]
    [Tooltip("index ของ landmark ที่ใช้เป็นตำแหน่งเคอร์เซอร์ — ค่าเริ่มต้น 0 = ข้อมือ (wrist) " +
              "เลือกจุดนี้เพราะตำแหน่งแทบไม่ขยับเวลาสลับท่ามือ (กำ/แบ) ต่างจากปลายนิ้วชี้ (8) ที่จะหุบเข้ามา " +
              "เมื่อกำมือ ทำให้เคอร์เซอร์เลื่อนหนีจากจุดที่ตั้งใจไว้ตอนกำลังจะกำ")]
    public int pointerLandmarkIndex = 0;

    bool mouseIsDown;

    #region Win32
    [DllImport("user32.dll")]
    static extern bool SetCursorPos(int x, int y);

    [DllImport("user32.dll")]
    static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);

    [DllImport("user32.dll")]
    static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    static extern bool ClientToScreen(IntPtr hWnd, ref POINT lpPoint);

    [StructLayout(LayoutKind.Sequential)]
    struct RECT { public int Left, Top, Right, Bottom; }

    [StructLayout(LayoutKind.Sequential)]
    struct POINT { public int X, Y; }

    const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
    const uint MOUSEEVENTF_LEFTUP = 0x0004;
    #endregion

    void Update()
    {
        var hub = HandTrackingHub.Instance;
        if (hub == null) return;

        int minLength = (pointerLandmarkIndex + 1) * 3;
        if (!hub.HandDetected || hub.Landmarks == null || hub.Landmarks.Length < minLength)
        {
            ReleaseMouseIfHeld();
            return;
        }

        MoveCursorToFingertip(hub.Landmarks);
        UpdateClickState(hub.CurrentGesture);
    }

    void MoveCursorToFingertip(float[] data)
    {
        float x = data[pointerLandmarkIndex * 3 + 0];
        float yUp = data[pointerLandmarkIndex * 3 + 1]; // ฝั่ง Python กลับแกนนี้มาแล้ว (h - y เดิม)

        float normX = mirrorX ? (1f - x / cameraFrameWidth) : (x / cameraFrameWidth);
        float fracFromTop = 1f - (yUp / cameraFrameHeight);

        normX = Mathf.Clamp01(normX);
        fracFromTop = Mathf.Clamp01(fracFromTop);

        IntPtr hWnd = Process.GetCurrentProcess().MainWindowHandle;
        if (hWnd == IntPtr.Zero) return;

        if (!GetClientRect(hWnd, out RECT rect)) return;

        int clientWidth = rect.Right - rect.Left;
        int clientHeight = rect.Bottom - rect.Top;
        if (clientWidth <= 0 || clientHeight <= 0) return;

        POINT origin = new POINT { X = 0, Y = 0 };
        if (!ClientToScreen(hWnd, ref origin)) return;

        int screenX = origin.X + Mathf.RoundToInt(normX * clientWidth);
        int screenY = origin.Y + Mathf.RoundToInt(fracFromTop * clientHeight);

        SetCursorPos(screenX, screenY);
    }

    void UpdateClickState(string gesture)
    {
        bool shouldBeDown = gesture == "fist";

        if (shouldBeDown && !mouseIsDown)
        {
            mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
            mouseIsDown = true;
        }
        else if (!shouldBeDown && mouseIsDown)
        {
            mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
            mouseIsDown = false;
        }
    }

    void ReleaseMouseIfHeld()
    {
        if (mouseIsDown)
        {
            mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
            mouseIsDown = false;
        }
    }

    void OnDisable() => ReleaseMouseIfHeld();
    void OnApplicationQuit() => ReleaseMouseIfHeld();
}
