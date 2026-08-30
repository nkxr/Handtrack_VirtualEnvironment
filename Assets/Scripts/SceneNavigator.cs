using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// ระบบนำทางกลาง ใช้ได้ทุกซีนโดยไม่ต้องเพิ่ม GameObject ในซีนอื่นเลย
/// (bootstrap ตัวเองก่อนซีนไหนๆ โหลดเสร็จ เหมือน HandTrackingHub)
///
/// - กด ESC จากซีนไหนก็ตาม (ที่ไม่ใช่ Home) จะพากลับไปที่ Home scene เสมอ
/// - ปุ่มต่างๆ ใน Home scene เรียก SceneNavigator.GoToScene(...) / QuitApp() ผ่านสคริปต์นี้
///
/// หมายเหตุ: ต้องเพิ่มซีนทั้งหมด (Home, SampleScene, birb, tah) ไว้ใน
/// File > Build Settings > Scenes In Build ก่อน ไม่งั้น SceneManager.LoadScene(ชื่อซีน)
/// จะหาไม่เจอ (ทั้งตอนกด Play ใน Editor และตอน build จริง)
/// </summary>
public class SceneNavigator : MonoBehaviour
{
    public const string HomeSceneName = "Home";

    static SceneNavigator instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        if (instance != null) return;
        var go = new GameObject("SceneNavigator");
        instance = go.AddComponent<SceneNavigator>();
        DontDestroyOnLoad(go);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            string current = SceneManager.GetActiveScene().name;
            if (current != HomeSceneName)
            {
                SceneManager.LoadScene(HomeSceneName);
            }
        }
    }

    public static void GoToScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    public static void QuitApp()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
