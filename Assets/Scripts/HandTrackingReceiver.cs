using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

/// <summary>
/// รับข้อมูลตำแหน่งจุดข้อมือ (21 landmarks) จากสคริปต์ Python ฝั่ง hand tracking ผ่าน UDP
/// แล้วเอาไปขยับตำแหน่งลูกบอล (marker) 21 ลูกในซีน เพื่อแสดงผล/ทดสอบว่าข้อมูลมาถึงจริง
///
/// รูปแบบข้อมูลที่รับ (JSON ส่งมาทาง UDP จากฝั่ง Python, ดู udp_sender.py):
///   {"hand_detected": true, "landmarks": [x0,y0,z0, x1,y1,z1, ..., x20,y20,z20]}
///
/// วิธีใช้: สร้าง Empty GameObject ในซีน แล้ว Add Component สคริปต์นี้เข้าไป
/// รันซีนแล้วเปิดฝั่ง Python (Main.py) ทิ้งไว้ ถ้าเชื่อมสำเร็จจะเห็นลูกบอล 21 ลูกขยับตามมือ
/// </summary>
public class HandTrackingReceiver : MonoBehaviour
{
    [Header("Network")]
    [Tooltip("พอร์ตที่ฟัง UDP (ต้องตรงกับพอร์ตที่ฝั่ง Python ส่งมา — ค่าเริ่มต้นในสคริปต์ Python คือ 5052)")]
    public int listenPort = 5052;

    [Header("Visualization")]
    [Tooltip("ลูกบอลแสดงจุด landmark ถ้าไม่ใส่ไว้ ระบบจะสร้าง Sphere ให้อัตโนมัติ 21 ลูก")]
    public GameObject markerPrefab;
    [Tooltip("สเกลแปลงจากพิกเซลภาพกล้อง (ประมาณ 0-1280, 0-720) เป็นหน่วยของ Unity")]
    public float positionScale = 0.01f;
    [Tooltip("จุดกึ่งกลางเฟรมกล้อง (กว้าง, สูง) ใช้เลื่อน landmark ให้อยู่รอบจุด origin ของวัตถุนี้")]
    public Vector2 frameCenter = new Vector2(640f, 360f);
    [Tooltip("ถ้าไม่มีข้อมูลใหม่เข้ามานานเกินนี้ (วินาที) จะซ่อน marker ทั้งหมด")]
    public float dataTimeout = 0.5f;

    public const int LandmarkCount = 21;

    Transform[] markers;
    UdpClient udpClient;
    Thread receiveThread;
    volatile bool running;

    readonly object dataLock = new object();
    float[] latestLandmarks;
    bool latestHandDetected;
    bool hasNewData;
    float lastDataTime;

    [Serializable]
    class HandData
    {
        public bool hand_detected;
        public int[] landmarks;
    }

    void Start()
    {
        CreateMarkersIfNeeded();
        StartReceiving();
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

    void StartReceiving()
    {
        running = true;
        receiveThread = new Thread(ReceiveLoop);
        receiveThread.IsBackground = true;
        receiveThread.Start();
    }

    void ReceiveLoop()
    {
        try
        {
            udpClient = new UdpClient(listenPort);
        }
        catch (Exception e)
        {
            Debug.LogError($"[HandTrackingReceiver] เปิดพอร์ต UDP {listenPort} ไม่ได้: {e.Message}");
            return;
        }

        IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
        while (running)
        {
            try
            {
                byte[] bytes = udpClient.Receive(ref remoteEndPoint);
                string json = Encoding.UTF8.GetString(bytes);
                HandData parsed = JsonUtility.FromJson<HandData>(json);
                if (parsed != null)
                {
                    lock (dataLock)
                    {
                        latestLandmarks = parsed.landmarks != null
                            ? Array.ConvertAll(parsed.landmarks, v => (float)v)
                            : null;
                        latestHandDetected = parsed.hand_detected;
                        hasNewData = true;
                    }
                }
            }
            catch (SocketException)
            {
                // เกิดตอนปิด socket ระหว่างหยุด thread ไม่ต้อง log เป็น error
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[HandTrackingReceiver] parse ข้อมูลที่รับมาไม่สำเร็จ: {e.Message}");
            }
        }
    }

    void Update()
    {
        float[] landmarksCopy = null;
        bool handDetected = false;
        bool newData;

        lock (dataLock)
        {
            newData = hasNewData;
            if (newData)
            {
                landmarksCopy = latestLandmarks;
                handDetected = latestHandDetected;
                hasNewData = false;
            }
        }

        if (newData)
        {
            if (handDetected && landmarksCopy != null && landmarksCopy.Length >= LandmarkCount * 3)
            {
                lastDataTime = Time.time;
                ApplyLandmarks(landmarksCopy);
                SetMarkersActive(true);
            }
            else
            {
                SetMarkersActive(false);
            }
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

            Vector3 localPos = new Vector3(
                (x - frameCenter.x) * positionScale,
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

    void StopReceiving()
    {
        running = false;
        udpClient?.Close();
        if (receiveThread != null && receiveThread.IsAlive)
        {
            receiveThread.Join(200);
        }
    }

    void OnDestroy()
    {
        StopReceiving();
    }

    void OnApplicationQuit()
    {
        StopReceiving();
    }
}
