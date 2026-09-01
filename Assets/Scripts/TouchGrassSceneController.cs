using UnityEngine;

/// <summary>
/// ตัวตั้งค่าซีน "Touch Grass 3D" ทั้งหมด — สร้างกล้อง (มุมก้มมองพื้น), แสงอาทิตย์, พื้นหญ้า,
/// แปลงหญ้า (GrassBladeField) และโครงมือ 3 มิติ (HandSkeleton3D) ทั้งหมดด้วยโค้ดตอน Awake()
/// เพื่อไม่ต้องพึ่งการอ้างอิง Material/Mesh/Prefab ที่ผูกไว้ในไฟล์ .unity ตรงๆ (ลดความเสี่ยงเรื่อง
/// GUID ผิดตอนแก้ไฟล์ซีนด้วยมือ)
/// </summary>
public class TouchGrassSceneController : MonoBehaviour
{
    [Header("มุมกล้อง (ยืนก้มมองหญ้า)")]
    public float cameraHeight = 1.5f;
    public float cameraPitchDegrees = 55f;
    public float fieldOfView = 60f;

    [Header("พื้นหญ้า")]
    public float groundSize = 10f; // เมตร

    [Header("แปลงหญ้าละเอียด (บิลบอร์ด ตรงหน้าที่มือเอื้อมถึง)")]
    public int bladeCount = 220;
    public float grassPatchRadius = 1.3f;
    public Vector3 grassPatchCenter = new Vector3(0f, 0f, 1.2f);

    void Awake()
    {
        // ทำแต่ละส่วนแยก try/catch กันไว้ — ถ้าส่วนไหนพลาด (เช่นหาเชดเดอร์ไม่เจอ) จะ log error แล้ว
        // ทำส่วนถัดไปต่อ แทนที่จะปล่อยให้ exception หยุดทั้ง Awake() จนของทั้งซีนหายหมดเหมือนที่เจอ
        SafeRun(SetupCamera, nameof(SetupCamera));
        SafeRun(SetupLight, nameof(SetupLight));
        SafeRun(SetupGround, nameof(SetupGround));

        GrassBladeField field = null;
        SafeRun(() =>
        {
            field = gameObject.AddComponent<GrassBladeField>();
            field.Initialize(bladeCount, grassPatchRadius, grassPatchCenter);
        }, nameof(GrassBladeField));

        GrassGroundDent dent = null;
        SafeRun(() =>
        {
            dent = gameObject.AddComponent<GrassGroundDent>();
            dent.Initialize(grassPatchCenter, grassPatchRadius * 2f * 1.15f);
        }, nameof(GrassGroundDent));

        SafeRun(() =>
        {
            HandSkeleton3D hand = gameObject.AddComponent<HandSkeleton3D>();
            hand.reachAreaCenter = new Vector3(grassPatchCenter.x, 0.35f, grassPatchCenter.z);
            hand.Initialize(field, dent);
        }, nameof(HandSkeleton3D));
    }

    static void SafeRun(System.Action action, string label)
    {
        try
        {
            action();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[TouchGrass3D] ส่วน '{label}' สร้างไม่สำเร็จ: {e}");
        }
    }

    void SetupCamera()
    {
        Camera cam = Camera.main;
        if (cam == null) cam = FindObjectOfType<Camera>();
        if (cam == null) return;

        cam.transform.position = new Vector3(0f, cameraHeight, 0f);
        cam.transform.rotation = Quaternion.Euler(cameraPitchDegrees, 0f, 0f);
        cam.orthographic = false;
        cam.fieldOfView = fieldOfView;
        cam.nearClipPlane = 0.03f;
        cam.farClipPlane = 100f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.55f, 0.75f, 0.85f); // ฟ้าอ่อนๆ กันจอมืดถ้ามองพ้นขอบพื้นหญ้า
    }

    void SetupLight()
    {
        GameObject lightGO = new GameObject("Sun Light");
        lightGO.transform.SetParent(transform, false);
        Light sun = lightGO.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.color = new Color(1f, 0.97f, 0.9f);
        sun.intensity = 1.25f;
        sun.shadows = LightShadows.Soft;
        lightGO.transform.rotation = Quaternion.Euler(55f, -25f, 0f);

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.4f, 0.45f, 0.35f);
    }

    void SetupGround()
    {
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "GrassGround";
        ground.transform.SetParent(transform, false);
        ground.transform.position = Vector3.zero;
        // Plane ปฐมภูมิของ Unity มีขนาดจริง 10x10 หน่วยที่ scale 1
        float scale = groundSize / 10f;
        ground.transform.localScale = new Vector3(scale, 1f, scale);

        Renderer renderer = ground.GetComponent<Renderer>();
        Texture2D tex = Resources.Load<Texture2D>("Textures/GrassGround");
        Material mat = CreateMaterial(false);
        if (tex != null)
        {
            mat.mainTexture = tex;
            mat.mainTextureScale = new Vector2(groundSize * 0.8f, groundSize * 0.8f);
        }
        // วาดพื้นก่อนใบหญ้าเสมอ (queue ต่ำกว่า) — ดู "ทำไมวาดก่อน/หลังถึงสำคัญ" ในคอมเมนต์ของ CreateMaterial
        mat.renderQueue = GroundRenderQueue;
        renderer.sharedMaterial = mat;
    }

    // Sprites/Default (เชดเดอร์หลักที่ใช้ตอนนี้) ไม่เขียนค่าลง depth buffer (ZWrite Off) เหมือนเชดเดอร์
    // ทึบทั่วไป ของที่ "วาดทีหลัง" จะทับของที่ "วาดก่อน" บนจอเสมอไม่ว่าตำแหน่ง 3 มิติจริงจะอยู่หน้าหรือ
    // หลังก็ตาม (painter's algorithm) ต้องคุม renderQueue เองให้พื้นวาดก่อน (เลขน้อยกว่า) แล้วค่อยวาด
    // ใบหญ้าทับทีหลัง (เลขมากกว่า) ไม่งั้นพื้นจะทับใบหญ้าจนมองไม่เห็นเหมือนที่เจอ
    public const int GroundRenderQueue = 2000;
    public const int BladeRenderQueue = 2100;

    /// <summary>
    /// สร้าง Material แบบ runtime — เดิมลอง "Universal Render Pipeline/Lit" เป็นตัวแรก แต่พบว่าตอน
    /// build จริง Unity สามารถ strip เชดเดอร์นี้ทิ้งได้เหมือนกันถ้าไม่มี Material asset ไหนในโปรเจกต์
    /// อ้างอิงมันไว้ตรงๆ (โปรเจกต์นี้ใช้ Sprite/TMP/UI ล้วน ไม่มีอะไรใช้ URP Lit เลยจริงๆ) ผลคือ
    /// Shader.Find คืน null ทุกตัวในลิสต์ แล้ว "new Material(null)" จะ throw exception ทำให้
    /// Awake() หยุดทำงานกลางคัน — อาการที่เจอคือทุกอย่างในซีนหายหมด เหลือแต่พื้นหลังกล้องสีฟ้า
    ///
    /// แก้โดยเรียงลำดับให้ลองเชดเดอร์ที่ "พิสูจน์แล้วว่าอยู่ในไฟล์ build จริงแน่ๆ" ก่อน — คือ
    /// Sprites/Default กับ UI/Default ซึ่งพิสูจน์ได้จากการที่ตัวเกม Flappy Bird (สไปรต์นก/ท่อ) และ
    /// เมนู Home/HUD ต่างๆ (Canvas/TMP) ที่ build ไปแล้วแสดงผลได้ปกติอยู่แล้ว จึงมั่นใจได้ว่าไม่โดน strip
    /// แน่นอน แล้วค่อยลอง URP/Unlit ทีหลังเป็นของแถม สุดท้ายกันเหนียวด้วยการห้าม shader เป็น null
    /// เด็ดขาดก่อนสร้าง Material (ถ้าหาไม่เจอจริงๆ จะ log error แทนที่จะปล่อยให้ throw)
    /// </summary>
    public static Material CreateMaterial(bool alphaCutout)
    {
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("UI/Default");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find(alphaCutout ? "Unlit/Transparent Cutout" : "Unlit/Texture");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Diffuse");

        bool isUrp = shader != null && shader.name.StartsWith("Universal Render Pipeline");

        if (shader == null)
        {
            Debug.LogError("[TouchGrass3D] หาเชดเดอร์ไม่เจอเลยสักตัว (ทุกตัวโดน strip ออกจาก build) " +
                "— ของในซีน Touch Grass 3D จะไม่ขึ้น ลองเพิ่มเชดเดอร์ที่ใช้เข้า Project Settings > " +
                "Graphics > Always Included Shaders");
            // กัน new Material(null) throw จนพัง Awake() ทั้งชุด — คืน Material เปล่าแบบไม่มี shader
            // ไม่ได้จริงๆ ใน Unity เลยต้องยอมใช้ shader ที่หาได้ตัวสุดท้ายที่สุด คือของ error เอง
            shader = Shader.Find("Hidden/InternalErrorShader");
        }

        Material mat = new Material(shader);

        if (alphaCutout)
        {
            if (isUrp && mat.HasProperty("_AlphaClip"))
            {
                mat.SetFloat("_AlphaClip", 1f);
                mat.EnableKeyword("_ALPHATEST_ON");
            }
            if (mat.HasProperty("_Cutoff")) mat.SetFloat("_Cutoff", 0.4f);
            if (mat.HasProperty("_Cull")) mat.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Off);
            // ใบหญ้าต้องวาดทีหลังพื้นเสมอ (เลข queue มากกว่า GroundRenderQueue) ไม่งั้นพื้นจะทับ
            // ใบหญ้าจนมองไม่เห็น (ดูคำอธิบายเต็มที่ GroundRenderQueue/BladeRenderQueue ด้านบน)
            mat.renderQueue = BladeRenderQueue;
        }

        return mat;
    }
}
