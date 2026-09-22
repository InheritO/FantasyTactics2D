using UnityEngine;

/// <summary>
/// 유닛 발밑에 표시되는 작은 세력색 표식. UnitHealthBar와 동일한 패턴으로,
/// 프리팹에 미리 준비할 필요 없이 스폰 시점에 코드로 생성된다.
/// </summary>
public class UnitFactionMarker : MonoBehaviour
{
    private const float MarkerSize = 0.3f;
    private static readonly Vector3 Offset = new Vector3(0f, -0.4f, 0f); // 발밑

    public void Initialize(UnitBase unit, Color factionColor)
    {
        transform.SetParent(unit.transform, false);
        transform.localPosition = Offset;

        SpriteRenderer sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sprite = ProceduralSpriteUtility.GetSquareSprite();
        sr.color = factionColor;
        sr.sortingLayerName = SortingLayers.UnitUI;
        sr.sortingOrder = 1; // 몸체(2)보다 아래에 그려지게
        transform.localScale = new Vector3(MarkerSize, MarkerSize, 1f);
    }
}