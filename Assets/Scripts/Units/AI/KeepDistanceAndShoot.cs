using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 원거리 유닛용 AI 행동. 적이 사거리 밖이면 다가가고,
/// 적이 너무 가까이 붙으면 사거리를 유지할 수 있는 타일로 물러난다.
/// 근접무기를 든 유닛이 이 행동을 받으면(원거리 부대에 섞여 들어온 경우),
/// 후퇴 로직이 의미 없으므로 방어형(DefensiveHoldPosition)으로 대체한다.
/// </summary>
public class KeepDistanceAndShoot : IUnitAIBehavior
{
    public void TakeTurn(UnitBase unit, GridManager gridManager, FactionData myFaction, List<UnitBase> enemyUnits)
    {
        bool isRangedUnit = unit.MainHandWeapon != null && unit.MainHandWeapon.isRanged;

        if (!isRangedUnit)
        {
            new DefensiveHoldPosition().TakeTurn(unit, gridManager, myFaction, enemyUnits);
            return;
        }

        UnitBase target = AIQueryUtility.FindNearestEnemy(unit, enemyUnits);

        if (target == null)
            return;

        int distance = gridManager.GetDistance(unit.GridCoord, target.GridCoord);

        // "너무 가까움"을 먼저 판단 (공격 가능 여부보다 우선)
        bool tooClose = distance < unit.AttackRange;

        if (tooClose)
        {
            Dictionary<Vector2Int, int> reachable =
                MovementRangeCalculator.CalculateReachableTiles(gridManager, unit.GridCoord, unit.MoveRange);

            if (reachable.Count > 0)
            {
                Vector2Int retreatTile = reachable.Keys
                    .OrderByDescending(coord => gridManager.GetDistance(coord, target.GridCoord))
                    .First();

                unit.TryMoveTo(retreatTile);
            }

            // 후퇴 후(혹은 후퇴할 곳이 없어서 제자리든) 여전히 사거리 안이면 쏨
            if (unit.IsInAttackRange(target))
                unit.TryAttack(target);

            return;
        }

        // 여기 도달했다는 건 "너무 가깝지 않다" = 딱 맞거나 먼 거리
        if (unit.IsInAttackRange(target))
        {
            unit.TryAttack(target);
            return;
        }

        // 사거리 밖 → 접근
        Dictionary<Vector2Int, int> approachReachable =
             MovementRangeCalculator.CalculateReachableTiles(gridManager, unit.GridCoord, unit.MoveRange);

        if (approachReachable.Count == 0)
            return;

        Vector2Int? idealTile = approachReachable.Keys
            .Where(coord => gridManager.GetDistance(coord, target.GridCoord) <= unit.AttackRange)
            .OrderByDescending(coord => gridManager.GetDistance(coord, target.GridCoord))
            .Cast<Vector2Int?>()
            .FirstOrDefault();

        Vector2Int bestTile = idealTile ?? approachReachable.Keys
            .OrderBy(coord => gridManager.GetDistance(coord, target.GridCoord))
            .First();

        int bestDistance = gridManager.GetDistance(bestTile, target.GridCoord);

        if (bestDistance < distance)
        {
            unit.TryMoveTo(bestTile);
            if (unit.IsInAttackRange(target))
                unit.TryAttack(target);
        }
    }
}