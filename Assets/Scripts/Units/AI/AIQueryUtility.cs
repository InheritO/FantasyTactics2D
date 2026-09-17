using UnityEngine;
using System.Collections.Generic;
using System.Linq;

//모든 AI 행동이 공유하는 "정보 수집" 로직
public static class AIQueryUtility
{
    public static UnitBase FindNearestEnemy(UnitBase unit, List<UnitBase> enemyUnits)
    {
        return enemyUnits
            .Where(u => u != null)
            .OrderBy(u => Vector2Int.Distance(u.GridCoord, unit.GridCoord))
            .FirstOrDefault();
    }
}