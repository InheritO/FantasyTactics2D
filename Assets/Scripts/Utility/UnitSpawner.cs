using UnityEngine;

/// <summary>
/// 유닛을 특정 좌표, 특정 세력으로 스폰하는 공용 로직.
/// 테스트 배치와 실제 전투 시작 시 초기 배치 양쪽에서 재사용된다.
/// </summary>
public static class UnitSpawner
{
    public static UnitBase Spawn(UnitBase unitPrefab, Vector2Int coord, FactionData faction,
         GridManager gridManager, BattleOutcomeManager outcomeManager, CombatLogger combatLogger)
    {
        TileInstance tile = gridManager.GetTile(coord);

        if (tile == null || !tile.IsWalkable())
        {
            Debug.Log($"스폰 실패: {coord}는 이동 불가 타일이거나 범위 밖입니다.");
            return null;
        }

        UnitBase unit = Object.Instantiate(unitPrefab);
        unit.SetFaction(faction);
        unit.PlaceOnGrid(coord, gridManager);

        if (!faction.isPlayerControlled)
            unit.AIBehavior = new AggressiveMoveTowardEnemy();

        outcomeManager.RegisterUnit(unit); // 추가: 사망 이벤트 구독
        combatLogger.RegisterUnit(unit);

        UnitActionVisual visual = unit.gameObject.AddComponent<UnitActionVisual>();
        visual.Initialize(unit, faction.factionColor);

        return unit;
    }
}