using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// สร้างหน้า Home Screen ทั้งหมดด้วยโค้ด (ไม่ต้องไปนั่งลาก UI ในซีน) — เหมือนแนวทางเดียวกับ
/// FlappyHandHUD.cs ปุ่มแต่ละปุ่มเรียก SceneNavigator เพื่อสลับซีน / ปิดโปรแกรม
/// </summary>
public class HomeMenu : MonoBehaviour
{
    [Header("ชื่อซีนต้องตรงกับชื่อไฟล์ .unity เป๊ะๆ และต้องอยู่ใน Build Settings")]
    public string puzzleSceneName = "tah";
    public string flappySceneName = "birb";
    public string handTestSceneName = "SampleScene";

    void Awake()
    {
        EnsureEventSystem();
        BuildUI();
    }

    void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }
    }

    void BuildUI()
    {
        GameObject canvasGO = new GameObject("HomeCanvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        CreateLabel(canvasGO.transform, "Title", "Hand Tracking Virtual Environment",
            new Vector2(0, 260), new Vector2(900, 90), 44, FontStyles.Bold);

        CreateLabel(canvasGO.transform, "Subtitle", "Choose a mode",
            new Vector2(0, 190), new Vector2(900, 50), 22, FontStyles.Normal);

        CreateButton(canvasGO.transform, "Puzzle (Jigsaw)", new Vector2(0, 90),
            () => SceneNavigator.GoToScene(puzzleSceneName));

        CreateButton(canvasGO.transform, "Flappy Bird", new Vector2(0, 10),
            () => SceneNavigator.GoToScene(flappySceneName));

        CreateButton(canvasGO.transform, "Hand Tracking Test", new Vector2(0, -70),
            () => SceneNavigator.GoToScene(handTestSceneName));

        CreateButton(canvasGO.transform, "Exit", new Vector2(0, -180),
            () => SceneNavigator.QuitApp(), new Color(0.55f, 0.12f, 0.12f, 0.95f));

        CreateLabel(canvasGO.transform, "Hint", "Press ESC anytime in-game to come back here",
            new Vector2(0, -280), new Vector2(900, 40), 16, FontStyles.Italic);
    }

    void CreateLabel(Transform parent, string name, string text, Vector2 pos, Vector2 size, int fontSize, FontStyles style)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
    }

    void CreateButton(Transform parent, string label, Vector2 pos, UnityEngine.Events.UnityAction onClick, Color? bgColor = null)
    {
        GameObject go = new GameObject("Button_" + label);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(340, 64);
        rt.anchoredPosition = pos;

        var img = go.AddComponent<Image>();
        img.color = bgColor ?? new Color(0.16f, 0.16f, 0.2f, 0.95f);

        var btn = go.AddComponent<Button>();
        var colors = btn.colors;
        colors.highlightedColor = new Color(1f, 1f, 1f, 0.25f);
        colors.pressedColor = new Color(1f, 1f, 1f, 0.4f);
        btn.colors = colors;
        btn.onClick.AddListener(onClick);

        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(go.transform, false);
        var textRt = textGO.AddComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;
        var tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 26;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
    }
}
