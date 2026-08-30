using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

/// <summary>
/// จุดรับข้อมูลมือกลางของทั้งเกม เปิด UDP socket แค่ตัวเดียวตลอดอายุเกม (อยู่ข้ามซีน)
/// ไม่ว่าอยู่ซีนไหนก็ดึงข้อมูลล่าสุดจากตัวนี้ตัวเดียวได้เลย ไม่ต้องเปิด socket เองต่อซีน
/// (แก้ปัญหาตอน build เป็นโปรแกรมจริงแล้วสลับซีนไปมา ที่เสี่ยง port ชนกันถ้าแต่ละซีนเปิด socket เอง)
///
/// สร้างตัวเองอัตโนมัติตอนเกมเริ่ม (ก่อนโหลดซีนแรกด้วยซ้ำ) ไม่ต้องลากเข้าไปใส่ในซีนไหนเลย
/// </summary>
public class HandTrackingHub : MonoBehaviour
{
    public static HandTrackingHub Instance { get; private set; }

    [Tooltip("ต้องตรงกับพอร์ตที่ Main.py ฝั่ง Python ส่ง landmark มา")]
    public int listenPort = 5052;

    public const int LandmarkCount = 21;

    /// <summary>มีมืออยู่ในเฟรมล่าสุดหรือไม่</summary>
    public bool HandDetected { get; private set; }

    /// <summary>สถานะท่ามือล่าสุด: "fist" (กำมือ) / "open" (แบมือ) / "other" (อื่นๆ) / "none" (ไม่พบมือ)</summary>
    public string CurrentGesture { get; private set; } = "none";

    /// <summary>จุด landmark ล่าสุด 21 จุด แบบแบน (x0,y0,z0, x1,y1,z1, ...) — null ถ้ายังไม่เคยได้ข้อมูล</summary>
    public float[] Landmarks { get; private set; }

    /// <summary>ยิงทีเดียวตอนตรวจพบ "กำมือครั้งใหม่" เท่านั้น (ต้องเคยหลุดจากสถานะกำมือไปก่อน ถึงจะนับใหม่
    /// กันคนกำมือค้างแล้วโดนนับเป็นกระโดดรัวๆ) — เอาไปผูกกับแอ็คชั่นในเกมได้เลย โดยไม่ต้องยุ่งกับ networking เอง</summary>
    public event Action OnFistJump;

    UdpClient udpClient;
    Thread receiveThread;
    volatile bool running;

    readonly object dataLock = new object();
    bool pendingHasData;
    bool pendingHandDetected;
    string pendingGesture = "none";
    float[] pendingLandmarks;

    string lastStableGestureForEdge = "none";

    [Serializable]
    class HandData
    {
        public bool hand_detected;
        public int[] landmarks;
        public string gesture;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        if (Instance != null) return;
        var go = new GameObject("HandTrackingHub");
        go.AddComponent<HandTrackingHub>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        StartReceiving();
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
            Debug.LogError($"[HandTrackingHub] เปิดพอร์ต UDP {listenPort} ไม่ได้: {e.Message}");
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
                        pendingHandDetected = parsed.hand_detected;
                        pendingGesture = string.IsNullOrEmpty(parsed.gesture) ? "none" : parsed.gesture;
                        pendingLandmarks = parsed.landmarks != null
                            ? Array.ConvertAll(parsed.landmarks, v => (float)v)
                            : null;
                        pendingHasData = true;
                    }
                }
            }
            catch (SocketException)
            {
                // เกิดตอนปิด socket ระหว่างหยุด thread ไม่ต้อง log เป็น error
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[HandTrackingHub] parse ข้อมูลที่รับมาไม่สำเร็จ: {e.Message}");
            }
        }
    }

    void Update()
    {
        bool hasData;
        bool handDetected = false;
        string gesture = null;
        float[] landmarks = null;

        lock (dataLock)
        {
            hasData = pendingHasData;
            if (hasData)
            {
                handDetected = pendingHandDetected;
                gesture = pendingGesture;
                landmarks = pendingLandmarks;
                pendingHasData = false;
            }
        }

        if (!hasData) return;

        HandDetected = handDetected;
        CurrentGesture = gesture;
        Landmarks = landmarks;

        // ยิง event ตอนเปลี่ยนจากสถานะอื่นมาเป็น "fist" ครั้งแรกเท่านั้น (edge-triggered)
        if (gesture == "fist" && lastStableGestureForEdge != "fist")
        {
            OnFistJump?.Invoke();
        }
        lastStableGestureForEdge = gesture;
    }

    void OnDestroy()
    {
        running = false;
        udpClient?.Close();
        if (receiveThread != null && receiveThread.IsAlive)
        {
            receiveThread.Join(200);
        }
        if (Instance == this) Instance = null;
    }

    void OnApplicationQuit()
    {
        running = false;
        udpClient?.Close();
    }
}
