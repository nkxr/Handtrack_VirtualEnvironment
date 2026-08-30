using UnityEngine;
using System.Collections.Generic;

public class PuzzleGenerator : MonoBehaviour
{
    public Transform puzzleBoard3D;
    public int rows = 3;
    public int cols = 3;

    [Header("3D Puzzle Size")]
    public float targetBoardWidth = 5f;
    public float targetBoardHeight = 4f;

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

        float pieceWidth = targetBoardWidth / cols;
        float pieceHeight = targetBoardHeight / rows;

        float startX = -targetBoardWidth / 2f + pieceWidth / 2f;
        float startY = -targetBoardHeight / 2f + pieceHeight / 2f;

        int tilePixelWidth = photo.width / cols;
        int tilePixelHeight = photo.height / rows;

        int borderThicknessX = Mathf.Max(1, Mathf.RoundToInt(tilePixelWidth * borderSizePercent));
        int borderThicknessY = Mathf.Max(1, Mathf.RoundToInt(tilePixelHeight * borderSizePercent));

        // เก็บตำแหน่งที่ถูกต้องเอาไว้สุ่ม
        List<Vector3> correctPositions = new List<Vector3>();

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                GameObject piece = GameObject.CreatePrimitive(PrimitiveType.Quad);
                piece.name = $"Piece_{row}_{col}";
                piece.transform.SetParent(puzzleBoard3D);

                // คำนวณตำแหน่งที่ถูกต้อง
                float posX = startX + (col * pieceWidth);
                float posY = startY + (row * pieceHeight);
                Vector3 correctPos = new Vector3(posX, posY, 0);

                piece.transform.localPosition = correctPos;
                correctPositions.Add(correctPos); // จำตำแหน่งเป้าหมายไว้ใน List

                piece.transform.localScale = new Vector3(pieceWidth, pieceHeight, 1);

                // --- 1. ใส่ Collider เพื่อให้คลิกได้ ---
                // (Quad มี MeshCollider มาให้ แต่สลับมาใช้ BoxCollider จะเสถียรกว่าสำหรับการคลิก)
                Destroy(piece.GetComponent<MeshCollider>());
                BoxCollider box = piece.AddComponent<BoxCollider>();
                box.size = new Vector3(1, 1, 0.1f);

                // --- 2. ใส่สคริปต์ PuzzlePiece และบอกว่าตำแหน่งที่ถูกต้องคือตรงไหน ---
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

        // --- 3. ระบบสุ่มตำแหน่ง (Shuffle) ---
        ShufflePuzzle(correctPositions);
    }

    private void ShufflePuzzle(List<Vector3> positions)
    {
        // อัลกอริทึมสลับตำแหน่ง (Fisher-Yates Shuffle)
        for (int i = 0; i < positions.Count; i++)
        {
            Vector3 temp = positions[i];
            int randomIndex = Random.Range(i, positions.Count);
            positions[i] = positions[randomIndex];
            positions[randomIndex] = temp;
        }

        // จับตำแหน่งที่ถูกสุ่มแล้ว ยัดกลับเข้าไปในแต่ละชิ้นส่วน
        int index = 0;
        foreach (Transform child in puzzleBoard3D)
        {
            child.localPosition = positions[index];
            index++;
        }
    }
}