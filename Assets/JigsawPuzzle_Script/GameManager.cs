using UnityEngine;
using UnityEngine.UI; // ใช้สำหรับ UI Text แบบธรรมดา
using TMPro; // 1. เพิ่มบรรทัดนี้เพื่อเรียกใช้งาน TextMeshPro
using UnityEngine.SceneManagement; // 1. เพิ่มบรรทัดนี้บนสุด เพื่อให้โหลดซีนใหม่ได้

public class GameManager : MonoBehaviour
{
    public static GameManager instance; // สร้าง Singleton เพื่อให้สคริปต์อื่นเรียกใช้ได้ง่ายๆ

    [Header("UI Elements")]
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI winText;
    public GameObject restartButton; // 2. เพิ่มตัวแปรสำหรับเก็บปุ่มเริ่มใหม่

    [Header("Effects")]
    public ParticleSystem fireworksEffect;

    private bool isTimerRunning = false;
    private float elapsedTime = 0f;
    private bool hasWon = false;

    void Awake()
    {
        // ตั้งค่า Instance
        if (instance == null) instance = this;
    }

    void Start()
    {
        // ซ่อนข้อความชนะตอนเริ่มเกม
        if (winText != null) winText.gameObject.SetActive(false);
        if (timerText != null) timerText.text = "00:00";
    }

    void Update()
    {
        // ถ้าจับเวลาอยู่และยังไม่ชนะ ให้นับเวลาไปเรื่อยๆ
        if (isTimerRunning && !hasWon)
        {
            elapsedTime += Time.deltaTime;
            UpdateTimerDisplay();
        }
    }

    void UpdateTimerDisplay()
    {
        if (timerText == null) return;

        // คำนวณเป็นนาทีและวินาที
        int minutes = Mathf.FloorToInt(elapsedTime / 60F);
        int seconds = Mathf.FloorToInt(elapsedTime - minutes * 60);

        // แสดงผลรูปแบบ MM:SS
        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    // ฟังก์ชันนี้จะถูกเรียกตอนที่ผู้เล่นคลิกจิ๊กซอว์ชิ้นแรก
    public void StartTimer()
    {
        if (!isTimerRunning && !hasWon)
        {
            isTimerRunning = true;
        }
    }

    // ฟังก์ชันนี้จะถูกเรียกตอนต่อจิ๊กซอว์เสร็จ
    public void TriggerWin()
    {
        if (hasWon) return; // ป้องกันการเรียกซ้ำ

        hasWon = true;
        isTimerRunning = false;

        // แสดงข้อความและเวลาที่ใช้ไป
        if (winText != null)
        {
            winText.gameObject.SetActive(true);
            winText.text = " YOU WIN! \nTime: " + timerText.text;
        }

        // เล่นเอฟเฟคพลุ
        if (fireworksEffect != null)
        {
            fireworksEffect.Play();
        }
        // 4. โชว์ปุ่มเริ่มใหม่ขึ้นมาเมื่อชนะ
        if (restartButton != null) restartButton.SetActive(true);
    }

    // 5. สร้างฟังก์ชันนี้ไว้ให้ปุ่มเรียกใช้งาน
    public void RestartGame()
    {
        // โหลดซีนปัจจุบันใหม่ตั้งแต่ต้น
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}