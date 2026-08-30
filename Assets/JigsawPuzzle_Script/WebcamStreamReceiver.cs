using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

/// <summary>
/// เชื่อมต่อไปยังสคริปต์ Python (video_streamer.py, รันจาก main_jigsaw.py) ผ่าน TCP
/// เพื่อรับภาพสดจากกล้อง — ใช้แทน WebCamTexture ของ Unity โดยตรง เพราะกล้องตัวเดียวกัน
/// เปิดพร้อมกันสองโปรเซส (Python ทำ hand tracking + Unity โชว์ภาพ) ไม่ได้
///
/// โปรโตคอล: รับความยาวเฟรมเป็น 4 byte big-endian (unsigned int) แล้วตามด้วยข้อมูล JPEG
/// ความยาวเท่านั้น ต่อกันไปเรื่อยๆ
///
/// วิธีใช้: WebCamManager จะเรียก GetComponent/AddComponent ตัวนี้ให้เองอัตโนมัติ
/// ไม่ต้องตั้งค่าอะไรในซีน แค่ต้องรัน main_jigsaw.py ฝั่ง Python ทิ้งไว้ก่อน (หรือระหว่าง)
/// รันซีนก็ได้ ระบบจะพยายามเชื่อมต่อใหม่ให้เองถ้ายังไม่เจอ
/// </summary>
public class WebcamStreamReceiver : MonoBehaviour
{
    [Tooltip("ต้องตรงกับ host ที่ video_streamer.py ฝั่ง Python เปิดไว้")]
    public string host = "127.0.0.1";
    [Tooltip("ต้องตรงกับพอร์ตที่ video_streamer.py ฝั่ง Python เปิดไว้ (ค่าเริ่มต้นในสคริปต์ Python คือ 5053)")]
    public int port = 5053;
    [Tooltip("ถ้าเชื่อมต่อไม่สำเร็จหรือหลุด จะลองใหม่ทุกกี่วินาที")]
    public float reconnectInterval = 1f;

    Thread receiveThread;
    volatile bool running;

    readonly object frameLock = new object();
    byte[] latestJpegBytes;
    bool hasNewFrame;

    Texture2D texture;

    /// <summary>Texture ล่าสุดที่ถอดรหัสแล้ว อัปเดตในตัวเองทุกครั้งที่มีเฟรมใหม่เข้ามา
    /// (อ้างอิง object เดิมตลอด ไม่เปลี่ยน reference ทุกเฟรม เอาไปผูกกับ RawImage.texture ครั้งเดียวได้)</summary>
    public Texture2D CurrentFrame => texture;

    /// <summary>มีภาพอย่างน้อยหนึ่งเฟรมมาถึงแล้วหรือยัง (ใช้เช็คก่อนจะแคปภาพจริง)</summary>
    public bool HasFrame { get; private set; }

    void Awake()
    {
        texture = new Texture2D(2, 2, TextureFormat.RGB24, false);
    }

    void OnEnable()
    {
        running = true;
        receiveThread = new Thread(ReceiveLoop);
        receiveThread.IsBackground = true;
        receiveThread.Start();
    }

    void ReceiveLoop()
    {
        while (running)
        {
            TcpClient client = null;
            try
            {
                client = new TcpClient();
                client.Connect(host, port);
                Debug.Log("[WebcamStreamReceiver] เชื่อมต่อไปยังฝั่ง Python สำเร็จ");

                using (NetworkStream stream = client.GetStream())
                {
                    byte[] header = new byte[4];
                    while (running)
                    {
                        ReadExact(stream, header, 4);
                        int length = (header[0] << 24) | (header[1] << 16) | (header[2] << 8) | header[3];
                        if (length <= 0 || length > 50_000_000) // กันข้อมูลเพี้ยน/หลุดโปรโตคอล
                            throw new IOException($"ความยาวเฟรมผิดปกติ: {length}");

                        byte[] jpg = new byte[length];
                        ReadExact(stream, jpg, length);

                        lock (frameLock)
                        {
                            latestJpegBytes = jpg;
                            hasNewFrame = true;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                if (running)
                    Debug.LogWarning($"[WebcamStreamReceiver] เชื่อมต่อไม่สำเร็จ/หลุด จะลองใหม่: {e.Message}");
            }
            finally
            {
                client?.Close();
            }

            if (running)
                Thread.Sleep(Mathf.RoundToInt(reconnectInterval * 1000));
        }
    }

    static void ReadExact(NetworkStream stream, byte[] buffer, int count)
    {
        int offset = 0;
        while (offset < count)
        {
            int read = stream.Read(buffer, offset, count - offset);
            if (read <= 0)
                throw new IOException("การเชื่อมต่อถูกปิดกลางทาง");
            offset += read;
        }
    }

    void Update()
    {
        byte[] jpgCopy = null;
        lock (frameLock)
        {
            if (hasNewFrame)
            {
                jpgCopy = latestJpegBytes;
                hasNewFrame = false;
            }
        }

        if (jpgCopy != null && texture.LoadImage(jpgCopy))
        {
            HasFrame = true;
        }
    }

    void OnDisable()
    {
        running = false;
        if (receiveThread != null && receiveThread.IsAlive)
        {
            receiveThread.Join(200);
        }
    }

    void OnDestroy()
    {
        running = false;
    }
}
