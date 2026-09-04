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
        UnitBase nearestEnemy = enemyUnits
            .Where(u => u != null)
            .OrderBy(u => gridManager.GetDistance(unit.GridCoord, u.GridCoord))
            .FirstOrDefault();

        if (nearestEnemy == null)
            return;

        // 이미 사거리 안이면 이동하지 않고 바로 공격
        if (unit.IsInAttackRange(nearestEnemy))
        {
            unit.TryAttack(nearestEnemy);
            return;
        }

        // 사거리 밖이면 최대한 접근
        Dictionary<Vector2Int, int> reachable =
            MovementRangeCalculator.CalculateReachableTiles(gridManager, unit.GridCoord, unit.MoveRange);

        if (reachable.Count == 0)
            return;

        Vector2Int bestTile = reachable.Keys
            .OrderBy(coord => gridManager.GetDistance(coord, nearestEnemy.GridCoord))
            .First();

        int currentDistance = gridManager.GetDistance(unit.GridCoord, nearestEnemy.GridCoord);
        int bestDistance = gridManager.GetDistance(bestTile, nearestEnemy.GridCoord);

        if (bestDistance < currentDistance)
        {
            unit.TryMoveTo(bestTile);

            // 이동 후 사거리 안에 들어왔으면 이어서 공격
            if (unit.IsInAttackRange(nearestEnemy))
                unit.TryAttack(nearestEnemy);
        }
    }
}