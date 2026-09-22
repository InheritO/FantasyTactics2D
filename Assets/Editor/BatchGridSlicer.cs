using System.Collections.Generic;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

/// <summary>
/// 선택한 텍스처들을 전부 64x64 격자로 한 번에 슬라이싱하는 에디터 도구.
/// Project 창에서 대상 PNG 파일들을 여러 개 선택한 다음 메뉴에서 실행한다.
/// </summary>
public static class BatchGridSlicer
{
    private const int CellWidth = 64;
    private const int CellHeight = 64;

    [MenuItem("Tools/LPC/Slice Selected Textures (64x64 Grid)")]
    private static void SliceSelected()
    {
        Texture2D[] textures = Selection.GetFiltered<Texture2D>(SelectionMode.Assets);

        if (textures.Length == 0)
        {
            Debug.LogWarning("슬라이싱할 텍스처를 Project 창에서 먼저 선택하세요.");
            return;
        }

        var factory = new SpriteDataProviderFactories();
        factory.Init();

        int processed = 0;

        foreach (var texture in textures)
        {
            string path = AssetDatabase.GetAssetPath(texture);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;

            if (importer == null)
                continue;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.SaveAndReimport();

            ISpriteEditorDataProvider dataProvider = factory.GetSpriteEditorDataProviderFromObject(texture);
            dataProvider.InitSpriteEditorDataProvider();

            ITextureDataProvider textureProvider = dataProvider.GetDataProvider<ITextureDataProvider>();
            textureProvider.GetTextureActualWidthAndHeight(out int width, out int height);

            List<SpriteRect> rects = new List<SpriteRect>();
            int cols = width / CellWidth;
            int rows = height / CellHeight;

            importer.isReadable = true; // 픽셀을 읽으려면 Read/Write가 켜져 있어야 함
            importer.SaveAndReimport();

            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < cols; col++)
                {
                    int y = height - (row + 1) * CellHeight;
                    int x = col * CellWidth;

                    if (IsCellEmpty(texture, x, y, CellWidth, CellHeight))
                        continue; // 완전히 투명한 칸은 건너뜀

                    rects.Add(new SpriteRect
                    {
                        rect = new Rect(x, y, CellWidth, CellHeight),
                        alignment = SpriteAlignment.Center,
                        pivot = new Vector2(0.5f, 0.5f),
                        name = $"{texture.name}_{row}_{col}",
                        spriteID = GUID.Generate()
                    });
                }
            }

            dataProvider.SetSpriteRects(rects.ToArray());
            dataProvider.Apply();
            importer.SaveAndReimport();


            importer.isReadable = false;
            importer.SaveAndReimport();

            processed++;
        }

        Debug.Log($"{processed}개 텍스처를 64x64 격자로 슬라이싱했습니다.");
    }

    private static bool IsCellEmpty(Texture2D texture, int x, int y, int width, int height)
    {
        Color[] pixels = texture.GetPixels(x, y, width, height);

        foreach (var pixel in pixels)
            if (pixel.a > 0.01f)
                return false;

        return true;
    }
}