using UnityEngine;

/// <summary>
/// 지정된 지형(예: 산)의 이동 비용을 할인해준다.
/// </summary>
[CreateAssetMenu(fileName = "TerrainAffinity", menuName = "Strategy/Traits/Terrain Affinity")]
public class TerrainAdaptationTrait : RacialTrait
{
    [Tooltip("할인이 적용될 지형 (예: TileType_Mountain 에셋)")]
    public TileTypeData targetTileType;
    [Tooltip("이동 비용 할인량 (최소 비용 1은 보장됨)")]
    [Min(1)]
    public int discount = 1;

    public override int ModifyMoveCost(TileInstance tile, int baseCost)
    {
        if (targetTileType != null && tile.TypeData == targetTileType)
            return Mathf.Max(1, baseCost - discount);

        return baseCost;
    }
}