using UnityEngine;
using System.Collections.Generic;

public class PuzzleGenerator : MonoBehaviour
{
    public Transform puzzleBoard3D;
    public int rows = 3;
    public int cols = 3;

    [Header("Border Settings")]
    [Range(0.01f, 0.2f)]
    public float borderSizePercent = 0.02f;
    public Color borderColor = Color.black;

    public void CreatePuzzle(Texture2D photo)
    {
        // ลบชิ้นส่วนเก่าออกก่อน
        foreach (Transform child in puzzleBoard3D)
        {
            Destroy(child.gameObject);
        }

        Camera mainCam = Camera.main;
        if (mainCam == null) return;

        // --- ส่วนที่แก้ไข: คำนวณขนาดหน้าจอที่แท้จริง ไม่ว่ากล้องจะอยู่ตรงไหน ---
        float dynamicHeight;

        // เช็คว่ากล้องเป็นแบบ 2D (Orthographic) หรือ 3D (Perspective)
        if (mainCam.orthographic)
        {
            dynamicHeight = 2f * mainCam.orthographicSize;
        }
        else
        {
            // คำนวณระยะห่างจากกล้อง ถึงจุดศูนย์กลาง (Z=0)
            float distance = Mathf.Abs(mainCam.transform.position.z);
            dynamicHeight = 2.0f * distance * Mathf.Tan(mainCam.fieldOfView * 0.5f * Mathf.Deg2Rad);
        }

        float dynamicWidth = dynamicHeight * mainCam.aspect;
        // -------------------------------------------------------------

        float pieceWidth = dynamicWidth / cols;
        float pieceHeight = dynamicHeight / rows;

        float startX = -dynamicWidth / 2f + pieceWidth / 2f;
        float startY = -dynamicHeight / 2f + pieceHeight / 2f;

        int tilePixelWidth = photo.width / cols;
        int tilePixelHeight = photo.height / rows;

        int borderThicknessX = Mathf.Max(1, Mathf.RoundToInt(tilePixelWidth * borderSizePercent));
        int borderThicknessY = Mathf.Max(1, Mathf.RoundToInt(tilePixelHeight * borderSizePercent));

        List<Vector3> correctPositions = new List<Vector3>();

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                GameObject piece = GameObject.CreatePrimitive(PrimitiveType.Quad);
                piece.name = $"Piece_{row}_{col}";
                piece.transform.SetParent(puzzleBoard3D);

                // --- ส่วนที่แก้ไข: ให้จิ๊กซอว์สร้างที่ Z=0 เสมอ ---
                float posX = startX + (col * pieceWidth);
                float posY = startY + (row * pieceHeight);
                Vector3 correctPos = new Vector3(posX, posY, 0f); // แก้กลับมาเป็น 0f

                piece.transform.localPosition = correctPos;
                correctPositions.Add(correctPos);

                piece.transform.localScale = new Vector3(pieceWidth, pieceHeight, 1);

                Destroy(piece.GetComponent<MeshCollider>());
                BoxCollider box = piece.AddComponent<BoxCollider>();
                box.size = new Vector3(1, 1, 0.1f);

                PuzzlePiece puzzleScript = piece.AddComponent<PuzzlePiece>();
                puzzleScript.correctPosition = correctPos;

                Color[] pixels = photo.GetPixels(col * tilePixelWidth, row * tilePixelHeight, tilePixelWidth, tilePixelHeight);

                for (int y = 0; y < tilePixelHeight; y++)
                {
                    for (int x = 0; x < tilePixelWidth; x++)
                    {
                        if (x < borderThicknessX || x >= tilePixelWidth - borderThicknessX ||
                            y < borderThicknessY || y >= tilePixelHeight - borderThicknessY)
                        {
                            int index = y * tilePixelWidth + x;
                            pixels[index] = borderColor;
                        }
                    }
                }

                Texture2D tileTexture = new Texture2D(tilePixelWidth, tilePixelHeight);
                tileTexture.SetPixels(pixels);
                tileTexture.Apply();

                Material mat = new Material(Shader.Find("Unlit/Texture"));
                mat.mainTexture = tileTexture;
                piece.GetComponent<Renderer>().material = mat;
            }
        }

        ShufflePuzzle(correctPositions);
    }

    private void ShufflePuzzle(List<Vector3> positions)
    {
        for (int i = 0; i < positions.Count; i++)
        {
            Vector3 temp = positions[i];
            int randomIndex = Random.Range(i, positions.Count);
            positions[i] = positions[randomIndex];
            positions[randomIndex] = temp;
        }

        int index = 0;
        foreach (Transform child in puzzleBoard3D)
        {
            child.localPosition = positions[index];
            index++;
        }
    }
}