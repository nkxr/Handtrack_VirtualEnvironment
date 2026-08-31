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
        SetupCamera();
        SetupLight();
        SetupGround();

        GrassBladeField field = gameObject.AddComponent<GrassBladeField>();
        field.Initialize(bladeCount, grassPatchRadius, grassPatchCenter);

        GrassGroundDent dent = gameObject.AddComponent<GrassGroundDent>();
        dent.Initialize(grassPatchCenter, grassPatchRadius * 2f * 1.15f);

        HandSkeleton3D hand = gameObject.AddComponent<HandSkeleton3D>();
        hand.reachAreaCenter = new Vector3(grassPatchCenter.x, 0.35f, grassPatchCenter.z);
        hand.Initialize(field, dent);
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
        renderer.sharedMaterial = mat;
    }

    /// <summary>
    /// สร้าง Material แบบ runtime โดยลองใช้เชดเดอร์ URP ก่อน (โปรเจกต์นี้ใช้ URP อยู่แล้ว) แล้ว
    /// ค่อย fallback ไป Unlit ธรรมดาถ้าหาไม่เจอ — เพื่อไม่ต้องพึ่งไฟล์ .mat ที่ผูก guid ไว้ตายตัว
    /// </summary>
    public static Material CreateMaterial(bool alphaCutout)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        bool isUrp = shader != null;
        if (shader == null) shader = Shader.Find(alphaCutout ? "Unlit/Transparent Cutout" : "Unlit/Texture");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Diffuse");

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
            mat.renderQueue = 2450; // AlphaTest queue กันปัญหาลำดับวาดทับกัน
        }

        return mat;
    }
}
