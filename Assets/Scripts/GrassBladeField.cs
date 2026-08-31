using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ปลูกกอหญ้า (บิลบอร์ดใบหญ้าแบบตัดขอบด้วยอัลฟ่า) เป็นแพทช์ตรงหน้ากล้อง แล้วให้ "โน้มเอน" เวลามีอะไร
/// (ปลายนิ้วของ HandSkeleton3D) เข้ามาใกล้ๆ เหมือนโดนมือจริงเกี่ยวผ่าน แล้วค่อยๆ สปริงกลับที่เดิมเอง
///
/// ทำงานแบบ runtime-only ทั้งหมด (ไม่ต้องมี prefab/material ที่ผูกไว้ในซีน) เพื่อลดความเสี่ยงเรื่อง
/// GUID อ้างอิงผิดตอนแก้ไฟล์ .unity ด้วยมือ — โหลด texture จาก Resources ตอน Awake แล้วสร้าง
/// Material/Mesh/GameObject ของใบหญ้าทั้งหมดเองในโค้ด
/// </summary>
public class GrassBladeField : MonoBehaviour
{
    class Blade
    {
        public Transform t;
        public Quaternion restRotation;
        public float bendAmount; // 0 = ตั้งตรง, 1 = โน้มสุด
        public Vector3 bendAxisWorld;
    }

    int bladeCount = 220;
    float patchRadius = 2.2f;
    Vector3 patchCenter = new Vector3(0, 0, 1.2f);

    [Header("ขนาดใบหญ้า (เมตร) — เตี้ยลงจากเดิมที่สูงเกิน 1 เมตร")]
    public float bladeHeight = 0.14f;
    public float bladeWidthRatio = 0.45f;
    public float minScale = 0.8f;
    public float maxScale = 1.2f;

    public float touchRadius = 0.09f;
    public float maxBendAngle = 45f; // ลดลงจาก 62 ให้ดูนุ่มนวลขึ้น ไม่ล้มสุดโต่งเกินไป
    public float bendSpring = 6f; // ยิ่งมากยิ่งสปริงกลับเร็ว

    readonly List<Blade> blades = new List<Blade>();
    Mesh sharedBladeMesh;

    public void Initialize(int count, float radius, Vector3 center)
    {
        bladeCount = count;
        patchRadius = radius;
        patchCenter = center;
        BuildField();
    }

    void BuildField()
    {
        sharedBladeMesh = BuildCrossQuadMesh();

        Texture2D bladeTex = Resources.Load<Texture2D>("Textures/GrassBlade");
        Material bladeMat = TouchGrassSceneController.CreateMaterial(true);
        if (bladeTex != null) bladeMat.mainTexture = bladeTex;

        GameObject fieldRoot = new GameObject("GrassBlades");
        fieldRoot.transform.SetParent(transform, false);

        for (int i = 0; i < bladeCount; i++)
        {
            // สุ่มตำแหน่งในวงกลม (uniform disk sampling)
            float ang = Random.Range(0f, Mathf.PI * 2f);
            float r = patchRadius * Mathf.Sqrt(Random.Range(0f, 1f));
            Vector3 pos = patchCenter + new Vector3(Mathf.Cos(ang) * r, 0f, Mathf.Sin(ang) * r);

            GameObject go = new GameObject("Blade_" + i);
            go.transform.SetParent(fieldRoot.transform, false);
            go.transform.position = pos;

            float yaw = Random.Range(0f, 360f);
            float scale = Random.Range(minScale, maxScale);
            go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            go.transform.localScale = Vector3.one * scale;

            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = sharedBladeMesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = bladeMat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            // ทำสีเข้ม/อ่อนต่างกันนิดหน่อยต่อกอ โดยไม่ต้อง instance material แยก (ประหยัด draw call)
            var mpb = new MaterialPropertyBlock();
            float tint = Random.Range(0.85f, 1.1f);
            mpb.SetColor("_BaseColor", new Color(tint, tint, tint, 1f));
            mpb.SetColor("_Color", new Color(tint, tint, tint, 1f));
            mr.SetPropertyBlock(mpb);

            blades.Add(new Blade
            {
                t = go.transform,
                restRotation = go.transform.rotation,
                bendAmount = 0f,
                bendAxisWorld = Vector3.forward,
            });
        }
    }

    /// <summary>สร้างเมชใบหญ้าแบบกากบาท (2 quad ตัดกัน 90 องศา) สูงตาม bladeHeight (เมตร) ฐานอยู่ที่ y=0 (pivot ล่าง)</summary>
    Mesh BuildCrossQuadMesh()
    {
        float h = bladeHeight;
        float w = bladeHeight * bladeWidthRatio;

        Vector3[] verts = new Vector3[]
        {
            // quad 1 (แนวหน้า-หลัง ตาม X)
            new Vector3(-w, 0, 0), new Vector3(w, 0, 0), new Vector3(w, h, 0), new Vector3(-w, h, 0),
            // quad 2 (หมุน 90 องศา ตาม Z)
            new Vector3(0, 0, -w), new Vector3(0, 0, w), new Vector3(0, h, w), new Vector3(0, h, -w),
        };
        Vector2[] uvs = new Vector2[]
        {
            new Vector2(0,0), new Vector2(1,0), new Vector2(1,1), new Vector2(0,1),
            new Vector2(0,0), new Vector2(1,0), new Vector2(1,1), new Vector2(0,1),
        };
        int[] tris = new int[]
        {
            0,2,1, 0,3,2,
            0,1,2, 0,2,3, // ฝั่งกลับด้าน ให้เห็นใบหญ้าจากทุกมุมแม้ material จะ cull
            4,6,5, 4,7,6,
            4,5,6, 4,6,7,
        };

        Mesh mesh = new Mesh();
        mesh.name = "GrassBladeCross";
        mesh.vertices = verts;
        mesh.uv = uvs;
        mesh.triangles = tris;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    /// <summary>เรียกทุกเฟรมจาก HandSkeleton3D โดยส่งตำแหน่งโลกของปลายนิ้วที่ต้องเช็คว่าไปแตะหญ้าไหม</summary>
    public void ReportTouchPoints(List<Vector3> worldPoints)
    {
        for (int i = 0; i < blades.Count; i++)
        {
            Blade b = blades[i];
            Vector3 basePos = b.t.position;
            bool touched = false;
            Vector3 pushDir = Vector3.zero;

            for (int p = 0; p < worldPoints.Count; p++)
            {
                Vector3 diff = basePos - worldPoints[p];
                diff.y = 0f;
                float dist = diff.magnitude;
                if (dist < touchRadius)
                {
                    touched = true;
                    Vector3 away = dist > 0.001f ? diff.normalized : Vector3.right;
                    float strength = 1f - (dist / touchRadius);
                    pushDir += away * strength;
                }
            }

            if (touched)
            {
                b.bendAmount = Mathf.Min(1f, b.bendAmount + Time.deltaTime * 10f);
                if (pushDir.sqrMagnitude > 0.0001f) b.bendAxisWorld = Vector3.Cross(Vector3.up, pushDir.normalized);
            }
            else
            {
                b.bendAmount = Mathf.Max(0f, b.bendAmount - Time.deltaTime * bendSpring * 0.3f);
            }
        }
    }

    void Update()
    {
        // สปริงกลับอย่างนุ่มนวลทุกเฟรม (รวมถึงตอนกำลังโน้น ให้ approach ค่าที่ตั้งไว้แบบ smooth)
        for (int i = 0; i < blades.Count; i++)
        {
            Blade b = blades[i];
            float angle = b.bendAmount * maxBendAngle;
            Quaternion bendRot = Quaternion.AngleAxis(angle, b.t.InverseTransformDirection(b.bendAxisWorld));
            Quaternion target = b.restRotation * bendRot;
            b.t.rotation = Quaternion.Slerp(b.t.rotation, target, Time.deltaTime * 8f);
        }
    }
}
