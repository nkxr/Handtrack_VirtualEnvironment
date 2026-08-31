using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// วาดโครงมือ 3 มิติ (21 จุดข้อต่อ + เส้นกระดูกเชื่อม ตามมาตรฐาน MediaPipe Hands) ขนาดพอๆ กับมือจริง
/// (~18-20 ซม. จากข้อมือถึงปลายนิ้วกลาง) แล้ววางลอยอยู่หน้ากล้องในพื้นที่แปลงหญ้า ให้ผู้เล่นเอื้อมมือ
/// จริงไปโบกเหมือนกำลังลูบ/จับหญ้าในโลกสามมิติ
///
/// การจับคู่พิกัด (ทายไว้ก่อน ยังไม่เคยเทสของจริง ปรับ public field ด้านล่างได้เลยถ้าความรู้สึกไม่ตรง):
///  - แกน X ของภาพกล้อง (ซ้าย-ขวา)   -> โลก X (ซ้าย-ขวา)
///  - แกน Y ของภาพกล้อง (บน-ล่างในเฟรม) -> โลก Y (สูง-ต่ำ เหนือพื้นหญ้า)
///  - z ของ landmark (ความลึกจากกล้อง) -> โลก Z (ยื่นมือเข้าหา/ออกจากแปลงหญ้า) — คาลิเบรตศูนย์อัตโนมัติ
///    จากค่า z ของข้อมือตอนเจอมือครั้งแรก เพราะค่า z ดิบไม่มีจุดศูนย์ตายตัว
/// </summary>
public class HandSkeleton3D : MonoBehaviour
{
    [Header("ความละเอียดเฟรมกล้อง (ต้องตรงกับฝั่ง Python)")]
    public int cameraFrameWidth = 1280;
    public int cameraFrameHeight = 720;

    [Header("พื้นที่เอื้อมมือ (โลก, หน่วยเมตร)")]
    public Vector3 reachAreaCenter = new Vector3(0f, 0.35f, 1.2f);
    public float reachHalfWidth = 1.1f;
    public float minHeight = 0.02f;
    public float maxHeight = 0.9f;
    public float reachHalfDepth = 0.5f;
    public float depthSensitivity = 220f; // ค่ายิ่งน้อย ยิ่งไวต่อการยื่น-ถอยมือ

    [Header("ตัวเลือกกลับด้าน (ลองสลับถ้ารู้สึกสวนทาง)")]
    public bool mirrorX = true;
    public bool invertHeight = false;

    [Header("ทิศทางที่มือหัน")]
    [Tooltip("แก้ปัญหามือหันเข้าจอ/เข้ากล้อง — หมุนรูปทรงมือ (ไม่ใช่ตำแหน่ง) ให้หันลงไปทางหญ้าแทน " +
              "ค่าเริ่มต้น (90,0,0) คือหมุนก้ม 90 องศารอบแกน X ถ้ายังหันผิดทางลองเปลี่ยนเป็น (-90,0,0)")]
    public Vector3 handOrientationEuler = new Vector3(90f, 0f, 0f);

    [Header("ขนาดมือจริง")]
    [Tooltip("ตัวคูณแปลงหน่วย landmark (พิกเซล) เป็นเมตร ปรับให้มือขนาดพอๆ กับมือจริงของผู้เล่น")]
    public float handScaleMetersPerPixel = 0.0011f;
    [Tooltip("ขยายขึ้นจากเดิมให้เห็นชัดขึ้น (ข้อต่อ/เส้นกระดูกใหญ่กว่าเดิมเกือบเท่าตัว)")]
    public float jointRadius = 0.014f;
    public float boneThickness = 0.011f;
    [Tooltip("สีมือ ตั้งใจให้สว่าง/อิ่มตัวกว่าสีผิวจริง เพื่อตัดกับพื้นหญ้าเขียวให้เห็นชัด")]
    public Color handColor = new Color(0.98f, 0.75f, 0.42f);

    GrassGroundDent groundDent;
    GrassBladeField grassField;
    Transform[] joints;
    Transform[] boneSegments;
    bool hasBaselineDepth;
    float baselineDepthZ;

    static readonly int[,] BONES = new int[,]
    {
        {0,1},{1,2},{2,3},{3,4},
        {0,5},{5,6},{6,7},{7,8},
        {5,9},{9,10},{10,11},{11,12},
        {9,13},{13,14},{14,15},{15,16},
        {13,17},{17,18},{18,19},{19,20},
        {0,17},
    };

    static readonly int[] FINGERTIPS = { 4, 8, 12, 16, 20 };

    public void Initialize(GrassBladeField field, GrassGroundDent dent = null)
    {
        grassField = field;
        groundDent = dent;
        BuildSkeletonVisual();
    }

    void BuildSkeletonVisual()
    {
        Material skinMat = TouchGrassSceneController.CreateMaterial(false);
        skinMat.color = handColor;
        if (skinMat.HasProperty("_BaseColor")) skinMat.SetColor("_BaseColor", handColor);
        if (skinMat.HasProperty("_EmissionColor"))
        {
            skinMat.EnableKeyword("_EMISSION");
            skinMat.SetColor("_EmissionColor", handColor * 0.4f);
            skinMat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }

        GameObject root = new GameObject("HandSkeletonVisual");
        root.transform.SetParent(transform, false);

        joints = new Transform[21];
        for (int i = 0; i < 21; i++)
        {
            GameObject jointGO = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            jointGO.name = "Joint_" + i;
            jointGO.transform.SetParent(root.transform, false);
            jointGO.transform.localScale = Vector3.one * (jointRadius * 2f);
            Object.Destroy(jointGO.GetComponent<Collider>());
            jointGO.GetComponent<Renderer>().sharedMaterial = skinMat;
            joints[i] = jointGO.transform;
        }

        int boneCount = BONES.GetLength(0);
        boneSegments = new Transform[boneCount];
        for (int i = 0; i < boneCount; i++)
        {
            GameObject boneGO = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            boneGO.name = "Bone_" + i;
            boneGO.transform.SetParent(root.transform, false);
            Object.Destroy(boneGO.GetComponent<Collider>());
            boneGO.GetComponent<Renderer>().sharedMaterial = skinMat;
            boneSegments[i] = boneGO.transform;
        }
    }

    void Update()
    {
        var hub = HandTrackingHub.Instance;
        if (hub == null || joints == null) return;

        if (!hub.HandDetected || hub.Landmarks == null || hub.Landmarks.Length < 21 * 3)
        {
            SetVisible(false);
            hasBaselineDepth = false;
            return;
        }

        SetVisible(true);
        ApplyPose(hub.Landmarks);
    }

    void SetVisible(bool visible)
    {
        if (joints == null) return;
        for (int i = 0; i < joints.Length; i++) if (joints[i] != null) joints[i].gameObject.SetActive(visible);
        for (int i = 0; i < boneSegments.Length; i++) if (boneSegments[i] != null) boneSegments[i].gameObject.SetActive(visible);
    }

    void ApplyPose(float[] data)
    {
        // ข้อมือ (landmark 0) กำหนดตำแหน่งฐานของมือทั้งหมดในโลก
        float wristX = data[0];
        float wristY = data[1];
        float wristZ = data[2];

        if (!hasBaselineDepth)
        {
            baselineDepthZ = wristZ;
            hasBaselineDepth = true;
        }

        float nx = Mathf.Clamp((wristX - cameraFrameWidth * 0.5f) / (cameraFrameWidth * 0.5f), -1f, 1f);
        float ny = Mathf.Clamp((wristY - cameraFrameHeight * 0.5f) / (cameraFrameHeight * 0.5f), -1f, 1f);
        float nz = Mathf.Clamp((wristZ - baselineDepthZ) / depthSensitivity, -1f, 1f);

        float worldX = reachAreaCenter.x + nx * reachHalfWidth * (mirrorX ? -1f : 1f);
        float heightT = Mathf.InverseLerp(-1f, 1f, invertHeight ? -ny : ny);
        float worldY = Mathf.Lerp(minHeight, maxHeight, heightT);
        float worldZ = reachAreaCenter.z + nz * reachHalfDepth;

        Vector3 wristWorld = new Vector3(worldX, worldY, worldZ);

        // จุดอื่นๆ ทั้งหมดวางตำแหน่งสัมพัทธ์กับข้อมือ ตามสัดส่วนจริงของมือ (ใช้ scale เดียวกันทั้ง x,y,z)
        // แล้วหมุนรูปทรงมือทั้งชุดด้วย handOrientationEuler ให้มือหันลงไปทางหญ้าแทนที่จะหันเข้ากล้อง
        Quaternion orientRot = Quaternion.Euler(handOrientationEuler);
        Vector3[] worldPos = new Vector3[21];
        worldPos[0] = wristWorld;
        for (int i = 1; i < 21; i++)
        {
            float dx = (data[i * 3 + 0] - wristX) * handScaleMetersPerPixel * (mirrorX ? -1f : 1f);
            float dy = (data[i * 3 + 1] - wristY) * handScaleMetersPerPixel;
            float dz = (data[i * 3 + 2] - wristZ) * handScaleMetersPerPixel;
            Vector3 localOffset = new Vector3(dx, dy, dz);
            worldPos[i] = wristWorld + orientRot * localOffset;
        }

        for (int i = 0; i < 21; i++) joints[i].position = worldPos[i];

        for (int i = 0; i < BONES.GetLength(0); i++)
        {
            Vector3 a = worldPos[BONES[i, 0]];
            Vector3 b = worldPos[BONES[i, 1]];
            Transform bone = boneSegments[i];
            Vector3 mid = (a + b) * 0.5f;
            float length = Vector3.Distance(a, b);
            bone.position = mid;
            bone.up = (b - a).sqrMagnitude > 0.000001f ? (b - a).normalized : Vector3.up;
            // Cylinder ปฐมภูมิของ Unity สูง 2 หน่วยตามแกน localScale.y=1 -> แปลงเป็นความยาว/2
            bone.localScale = new Vector3(boneThickness, Mathf.Max(length * 0.5f, 0.0001f), boneThickness);
        }

        if (grassField != null || groundDent != null)
        {
            var tips = new List<Vector3>(FINGERTIPS.Length);
            for (int i = 0; i < FINGERTIPS.Length; i++) tips.Add(worldPos[FINGERTIPS[i]]);
            grassField?.ReportTouchPoints(tips);
            groundDent?.ReportPressPoints(tips);
        }
    }
}
