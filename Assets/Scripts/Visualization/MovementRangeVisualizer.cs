using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 계산된 이동 가능 타일들을 씬에 하이라이트로 표시한다.
/// </summary>
public class MovementRangeVisualizer : MonoBehaviour
{
    public Color highlightColor = new Color(1f, 1f, 0f, 0.4f);

    private List<GameObject> highlightObjects = new List<GameObject>();
    private GridManager gridManager;

    public void Setup(GridManager grid)
    {
        gridManager = grid;
    }

    public void ShowRange(Dictionary<Vector2Int, int> reachableTiles)
    {
        ClearRange();

        foreach (var coord in reachableTiles.Keys)
        {
            GameObject highlight = CreateHighlightTile(coord);
            highlightObjects.Add(highlight);
        }
    }

    public void ClearRange()
    {
        foreach (var obj in highlightObjects)
        {
            if (obj != null)
                Destroy(obj);
        }
        highlightObjects.Clear();
    }

    private GameObject CreateHighlightTile(Vector2Int coord)
    {
        GameObject obj = new GameObject($"Highlight_{coord.x}_{coord.y}");
        obj.transform.parent = this.transform;
        obj.transform.position = gridManager.GridToWorld(coord);

        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = CreateSquareSprite();
        sr.color = highlightColor;
        sr.sortingOrder = 0; // 타일 위, 유닛 아래 정도로 조정 필요시 값 변경

        return obj;
    }

    private Sprite cachedSprite;
    private Sprite CreateSquareSprite()
    {
        if (cachedSprite == null)
        {
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            cachedSprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        }
        return cachedSprite;
    }
}