using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// พื้นหญ้าแผ่นละเอียด (mesh grid ปรับความสูงจุดต่อจุดได้) วางซ้อนบนพื้นหญ้าหลักตรงบริเวณแปลงหญ้า
/// เพื่อทำให้เห็น "รอยบุ๋ม" เวลานิ้วกดลงใกล้พื้น — แต่ละนิ้วที่กดจะดันจุดตาข่ายรอบๆ ให้ยุบลงเป็นแอ่ง
/// (คล้ายรอยฝ่ามือกดหญ้า) แล้วค่อยๆ สปริงตัวกลับเป็นพื้นเรียบเมื่อมือขยับออกหรือยกขึ้น
/// </summary>
public class GrassGroundDent : MonoBehaviour
{
    [Header("ขนาดแผ่นที่ยุบได้ (เมตร) และความละเอียดตาข่าย")]
    public float patchSize = 3.0f;
    public int resolution = 40;

    [Header("ความบุ๋ม")]
    public float maxDentDepth = 0.045f;
    public float pressRadius = 0.12f;
    [Tooltip("นิ้วต้องอยู่ต่ำกว่าความสูงนี้ (เมตรจากพื้น) ถึงจะนับว่า 'กด' ลงหญ้า")]
    public float pressHeightThreshold = 0.16f;
    public float springSpeed = 7f;
    public float textureTiling = 4f;

    Mesh mesh;
    Transform meshTransform;
    Vector3[] baseVerts;
    Vector3[] currentVerts;
    float[] targetOffset;
    int vRes;

    public void Initialize(Vector3 patchCenter, float sizeHint = -1f)
    {
        if (sizeHint > 0f) patchSize = sizeHint;
        BuildMesh(patchCenter);
    }

    void BuildMesh(Vector3 patchCenter)
    {
        GameObject go = new GameObject("GrassGroundDentMesh");
        go.transform.SetParent(transform, false);
        go.transform.position = new Vector3(patchCenter.x, 0.002f, patchCenter.z);
        meshTransform = go.transform;

        vRes = resolution + 1;
        int vertCount = vRes * vRes;
        baseVerts = new Vector3[vertCount];
        currentVerts = new Vector3[vertCount];
        targetOffset = new float[vertCount];
        Vector2[] uvs = new Vector2[vertCount];

        float half = patchSize * 0.5f;
        for (int z = 0; z < vRes; z++)
        {
            for (int x = 0; x < vRes; x++)
            {
                int idx = z * vRes + x;
                float px = -half + (patchSize * x) / resolution;
                float pz = -half + (patchSize * z) / resolution;
                baseVerts[idx] = new Vector3(px, 0f, pz);
                currentVerts[idx] = baseVerts[idx];
                uvs[idx] = new Vector2(px / textureTiling + 0.5f, pz / textureTiling + 0.5f);
            }
        }

        int[] tris = new int[resolution * resolution * 6];
        int t = 0;
        for (int z = 0; z < resolution; z++)
        {
            for (int x = 0; x < resolution; x++)
            {
                int i0 = z * vRes + x;
                int i1 = i0 + 1;
                int i2 = i0 + vRes;
                int i3 = i2 + 1;
                tris[t++] = i0; tris[t++] = i2; tris[t++] = i1;
                tris[t++] = i1; tris[t++] = i2; tris[t++] = i3;
            }
        }

        mesh = new Mesh();
        mesh.name = "GrassGroundDentMesh";
        if (vertCount > 65000) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.vertices = currentVerts;
        mesh.uv = uvs;
        mesh.triangles = tris;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        var mf = go.AddComponent<MeshFilter>();
        mf.mesh = mesh;
        var mr = go.AddComponent<MeshRenderer>();
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        Texture2D tex = Resources.Load<Texture2D>("Textures/GrassGround");
        Material mat = TouchGrassSceneController.CreateMaterial(false);
        if (tex != null) mat.mainTexture = tex;
        mr.sharedMaterial = mat;
    }

    /// <summary>เรียกทุกเฟรมจาก HandSkeleton3D ส่งตำแหน่งโลกของปลายนิ้ว (รวมความสูง y) มาเช็คว่ากดลงหญ้าไหม</summary>
    public void ReportPressPoints(List<Vector3> worldFingertips)
    {
        if (mesh == null) return;

        for (int i = 0; i < baseVerts.Length; i++)
        {
            Vector3 worldVert = meshTransform.TransformPoint(baseVerts[i]);
            float deepest = 0f;

            for (int p = 0; p < worldFingertips.Count; p++)
            {
                Vector3 fp = worldFingertips[p];
                if (fp.y > pressHeightThreshold) continue; // นิ้วยังยกอยู่ ไม่กด

                float dx = worldVert.x - fp.x;
                float dz = worldVert.z - fp.z;
                float dist = Mathf.Sqrt(dx * dx + dz * dz);
                if (dist >= pressRadius) continue;

                float falloff = 1f - Mathf.SmoothStep(0f, 1f, dist / pressRadius);
                float depthT = 1f - Mathf.Clamp01(fp.y / pressHeightThreshold); // ยิ่งกดต่ำ ยิ่งบุ๋มลึก
                float amount = falloff * depthT;
                if (amount > deepest) deepest = amount;
            }

            targetOffset[i] = -maxDentDepth * deepest;
        }
    }

    void Update()
    {
        if (mesh == null) return;
        bool changed = false;
        for (int i = 0; i < currentVerts.Length; i++)
        {
            float targetY = baseVerts[i].y + targetOffset[i];
            float newY = Mathf.Lerp(currentVerts[i].y, targetY, Time.deltaTime * springSpeed);
            if (Mathf.Abs(newY - currentVerts[i].y) > 0.0001f) changed = true;
            currentVerts[i].y = newY;
        }

        if (changed)
        {
            mesh.vertices = currentVerts;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
        }
    }
}
