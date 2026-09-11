using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 제자리를 지키다가, 사거리 안에 적이 들어오면 공격한다.
/// 적이 사거리 밖에 있으면 먼저 다가가지 않는다.
/// </summary>
public class DefensiveHoldPosition : IUnitAIBehavior
{
    public void TakeTurn(UnitBase unit, GridManager gridManager, FactionData myFaction, List<UnitBase> enemyUnits)
    {
        UnitBase target = AIQueryUtility.FindNearestEnemy(unit, enemyUnits);

        if (target == null)
            return;

        if (unit.IsInAttackRange(target))
            unit.TryAttack(target);

        // 사거리 밖이면 아무것도 하지 않고 제자리를 지킴
    }
}