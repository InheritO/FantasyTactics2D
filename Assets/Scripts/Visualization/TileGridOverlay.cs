using UnityEngine;
using System.Collections.Generic;
using NaughtyAttributes;

/// <summary>
/// 타일 경계를 표시하는 얇은 테두리 오버레이. 지형 스프라이트는 꽉 채워서 이어지게 두고,
/// 격자 표시만 별도 레이어로 그 위에 얹는다.
/// </summary>
public class TileGridOverlay : MonoBehaviour
{
    public GridManager gridManager;
    public Color lineColor = new Color(0f, 0f, 0f, 0.35f);

    private List<GameObject> lineObjects = new List<GameObject>();

    [Button]
    public void Build()
    {
        Clear();

        for (int x = 0; x < gridManager.width; x++)
        {
            for (int y = 0; y < gridManager.height; y++)
            {
                GameObject obj = new GameObject($"GridLine_{x}_{y}");
                obj.transform.parent = transform;
                obj.transform.position = gridManager.GridToWorld(new Vector2Int(x, y));

                SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
                sr.sprite = ProceduralSpriteUtility.GetOutlineSprite();
                sr.color = lineColor;
                sr.sortingLayerName = SortingLayers.Highlight;
                sr.sortingOrder = 10; // 이동범위 하이라이트(1)보다 위, 격자선은 항상 보이게
            }
        }
    }

    [Button]
    public void Clear()
    {
        foreach (var obj in lineObjects)
            if (obj != null)
                Destroy(obj);

        lineObjects.Clear();
    }
}