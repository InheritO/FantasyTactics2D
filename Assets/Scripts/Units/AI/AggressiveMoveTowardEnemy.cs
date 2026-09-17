using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 가장 가까운 적 유닛을 향해, 이동 범위 내에서 최대한 접근하는 기본 AI 행동.
/// </summary>
public class AggressiveMoveTowardEnemy : IUnitAIBehavior
{
    public void TakeTurn(UnitBase unit, GridManager gridManager, FactionData myFaction, List<UnitBase> enemyUnits)
    {
        Debug.Log($"[AI-TakeTurn] {unit.name} 시작. enemyUnits 수: {enemyUnits.Count}");

        UnitBase nearestEnemy = AIQueryUtility.FindNearestEnemy(unit, enemyUnits);

        if (nearestEnemy == null)
            return;

        Debug.Log($"[AI-TakeTurn] {unit.name} → 최근접 적: {nearestEnemy.name}, 거리: {gridManager.GetDistance(unit.GridCoord, nearestEnemy.GridCoord)}");
        
        // 재장전 확인
        if (unit.MainHandWeapon != null && unit.MainHandWeapon.requiresReload && !unit.IsLoaded)
        {
            unit.PerformReload();
            return;
        }


        // 이미 사거리 안이면 이동하지 않고 바로 공격
        if (unit.IsInAttackRange(nearestEnemy))
        {
            Debug.Log($"[AI-TakeTurn] {unit.name}: 사거리 안, 공격 시도");
            unit.TryAttack(nearestEnemy);
            return;
        }

        // 사거리 밖이면 최대한 접근
        Dictionary<Vector2Int, int> reachable =
            MovementRangeCalculator.CalculateReachableTiles(gridManager, unit);

        Debug.Log($"[AI-TakeTurn] {unit.name}: 사거리 밖, 이동 가능 타일 수: {reachable.Count}");

        if (reachable.Count == 0)
            return;

        Vector2Int bestTile = reachable.Keys
            .OrderBy(coord => gridManager.GetDistance(coord, nearestEnemy.GridCoord))
            .First();

        int currentDistance = gridManager.GetDistance(unit.GridCoord, nearestEnemy.GridCoord);
        int bestDistance = gridManager.GetDistance(bestTile, nearestEnemy.GridCoord);

        Debug.Log($"[AI-TakeTurn] {unit.name}: 현재거리 {currentDistance} → 최적타일거리 {bestDistance}");

        if (bestDistance < currentDistance)
        {
            unit.TryMoveTo(bestTile);

            if (unit.IsInAttackRange(nearestEnemy))
                unit.TryAttack(nearestEnemy);
        }
        else
        {
            Debug.Log($"[AI-TakeTurn] {unit.name}: 더 가까워질 방법이 없어 이동하지 않음.");
        }
    }
}