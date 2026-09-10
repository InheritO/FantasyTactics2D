using UnityEngine;

/// <summary>
/// 유닛을 특정 좌표, 특정 세력으로 스폰하는 공용 로직.
/// 테스트 배치와 실제 전투 시작 시 초기 배치 양쪽에서 재사용된다.
/// </summary>
public static class UnitSpawner
{
    public static UnitBase Spawn(UnitBase unitPrefab, Vector2Int coord, FactionData faction, RosterEntry entry,
     GridManager gridManager, BattleOutcomeManager outcomeManager, CombatLogger combatLogger = null)
    {
        UnitBase unit = Spawn(unitPrefab, coord, faction, gridManager, outcomeManager, combatLogger);

        if (unit != null)
            unit.ApplyLoadout(entry);

        return unit;
    }

    public static UnitBase Spawn(UnitBase unitPrefab, Vector2Int coord, FactionData faction,
         GridManager gridManager, BattleOutcomeManager outcomeManager, CombatLogger combatLogger = null)
    {
        if (unitPrefab == null)
        {
            Debug.LogError("스폰 실패: unitPrefab이 null입니다.");
            return null;
        }

        if (faction == null)
        {
            Debug.LogError("스폰 실패: faction이 null입니다.");
            return null;
        }

        if (faction.race == null)
            Debug.LogWarning($"[{faction.factionName}] Race가 설정되지 않았습니다. 스탯이 기본값(0)으로 처리됩니다.");

        if (gridManager == null)
        {
            Debug.LogError("스폰 실패: gridManager가 null입니다.");
            return null;
        }

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

        if (outcomeManager != null)
            outcomeManager.RegisterUnit(unit); // 사망 이벤트 구독
        else
            Debug.LogWarning("outcomeManager가 연결되지 않아 이 유닛의 사망이 승패 판정에 반영되지 않습니다.");

        // combatLogger는 디버그 전용 도구이므로 없어도 스폰 자체는 계속 진행함
        if (combatLogger != null)
            combatLogger.RegisterUnit(unit);

        UnitActionVisual visual = unit.gameObject.AddComponent<UnitActionVisual>();
        visual.Initialize(unit, faction.factionColor);

        UnitHealthBar healthBar = new GameObject("HealthBar").AddComponent<UnitHealthBar>();
        healthBar.Initialize(unit);

        return unit;
    }

    // 대전 모드 전용 스폰 경로. SkirmishParticipant의 종족을 명시적으로 부여한다.
    public static UnitBase Spawn(UnitBase unitPrefab, Vector2Int coord, SkirmishParticipant participant, RosterEntry entry,
         GridManager gridManager, BattleOutcomeManager outcomeManager, CombatLogger combatLogger = null)
    {
        if (participant == null)
        {
            Debug.LogError("스폰 실패: participant가 null입니다.");
            return null;
        }

        UnitBase unit = Spawn(unitPrefab, coord, participant.Faction, gridManager, outcomeManager, combatLogger);

        if (unit != null)
        {
            unit.AssignRace(participant.Race);
            unit.ApplyLoadout(entry);
        }

        return unit;
    }
}