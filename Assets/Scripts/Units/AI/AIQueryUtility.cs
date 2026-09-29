using UnityEngine;
using System.Collections.Generic;
using System.Linq;

// 모든 AI 행동이 공유하는 "정보 수집" 로직
public static class AIQueryUtility
{
    public static UnitBase FindNearestEnemy(UnitBase unit, List<UnitBase> enemyUnits)
    {
        return enemyUnits
            .Where(u => u != null)
            .OrderBy(u => Vector2Int.Distance(u.GridCoord, unit.GridCoord))
            .FirstOrDefault();
    }

    // from -> to로 이동하면 기회공격(ZOC)을 맞는지 여부
    public static bool WouldTriggerOpportunityAttack(UnitBase mover, Vector2Int from, Vector2Int to, GridManager gridManager)
    {
        UnitBase[] allUnits = Object.FindObjectsByType<UnitBase>();
        return ZoneOfControlResolver.GetOpportunityAttackers(mover, from, to, gridManager, allUnits).Count > 0;
    }

    // candidates 중 ZOC를 유발하지 않는 것만 골라낸다. 전부 위험하면(안전한 선택지가 없으면) 원래 목록을 그대로 반환.
    public static IEnumerable<Vector2Int> PreferZocSafeTiles(UnitBase mover, Vector2Int from, IEnumerable<Vector2Int> candidates, GridManager gridManager)
    {
        var safe = candidates.Where(coord => !WouldTriggerOpportunityAttack(mover, from, coord, gridManager)).ToList();
        return safe.Count > 0 ? safe : candidates;
    }

    // candidates 중 엄폐가 제공되는 위치를 우선한다. 없으면 원래 목록을 그대로 반환.
    public static IEnumerable<Vector2Int> PreferCoveredTiles(IEnumerable<Vector2Int> candidates, GridManager gridManager)
    {
        var covered = candidates.Where(coord => CombatResolver.HasCoverAt(coord, gridManager)).ToList();
        return covered.Count > 0 ? covered : candidates;
    }
}