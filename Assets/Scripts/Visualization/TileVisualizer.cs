using UnityEngine;
using NaughtyAttributes;

/// <summary>
/// 생성된 맵 데이터를 실제 GameObject로 씬에 배치해서 눈으로 확인하기 위한 클래스.
/// 프리팹 없이 기본 Quad + 색상으로 빠르게 시각화한다.
/// </summary>
public class TileVisualizer : MonoBehaviour
{
    [Header("References")]
    public GridManager gridManager;

    [Header("Visual Settings")]
    public float tileVisualSize = 0.9f; // tileSize보다 살짝 작게 해서 타일 사이 경계선이 보이게 함

    private GameObject[,] tileObjects;

    void Start()
    {
        VisualizeMap();
    }

    public void VisualizeMap()
    {
        ClearVisuals();

        int width = gridManager.width;
        int height = gridManager.height;
        tileObjects = new GameObject[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                TileInstance tile = gridManager.GetTile(new Vector2Int(x, y));
                CreateTileObject(tile);
            }
        }
    }

    private void CreateTileObject(TileInstance tile)
    {
        GameObject tileObj = new GameObject($"Tile_{tile.GridCoord.x}_{tile.GridCoord.y}_{tile.TypeData.tileName}");
        tileObj.transform.parent = this.transform;
        tileObj.transform.position = gridManager.GridToWorld(tile.GridCoord);
        tileObj.transform.localScale = Vector3.one * tileVisualSize;

        SpriteRenderer sr = tileObj.AddComponent<SpriteRenderer>();

        if (tile.TypeData.icon != null)
        {
            sr.sprite = tile.TypeData.icon; // 실제 타일 스프라이트가 있으면 사용
        }
        else
        {
            sr.sprite = GetDefaultSquareSprite(); // 없으면 기본 흰색 사각형 스프라이트 + 색상
            sr.color = tile.TypeData.previewColor;
        }

        tileObjects[tile.GridCoord.x, tile.GridCoord.y] = tileObj;
    }

    // Unity 기본 제공 흰색 사각형 스프라이트 (Sprites/Default 셰이더용)
    private Sprite defaultSquareSprite;
    private Sprite GetDefaultSquareSprite()
    {
        if (defaultSquareSprite == null)
        {
            // 1x1 흰색 텍스처를 코드로 직접 생성
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();

            // 텍스처를 스프라이트로 변환 (pivot 중앙, pixelsPerUnit 1)
            defaultSquareSprite = Sprite.Create(
                texture,
                new Rect(0, 0, 1, 1),
                new Vector2(0.5f, 0.5f),
                1f
            );
        }

        return defaultSquareSprite;
    }

    public void ClearVisuals()
    {
        if (tileObjects == null) return;

        foreach (var obj in tileObjects)
        {
            if (obj != null)
                Destroy(obj);
        }

        tileObjects = null;
    }
}