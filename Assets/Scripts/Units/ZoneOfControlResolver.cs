using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 유닛이 이동할 때, 적의 근접 사거리(ZOC)를 벗어나면서 발생하는 기회공격 대상을 판정한다.
/// CombatResolver/MovementRangeCalculator와 같은 패턴: 상태를 갖지 않는 순수 계산 클래스.
/// "누가 기회공격을 하는가"만 판단하고, 실제 공격 적용은 호출한 쪽(UnitBase.TryMoveTo)이 담당한다.
/// </summary>
public static class ZoneOfControlResolver
{
    // startCoord에선 이 유닛과 인접해 있었지만 endCoord에서는 더 이상 인접하지 않은 적 유닛 목록을 반환한다.
    // (이동 경로 중간을 스쳐 지나가는 경우는 판정하지 않음 - 시작/도착 두 지점만 비교하는 단순화된 버전)
    public static List<UnitBase> GetOpportunityAttackers(
        UnitBase mover, Vector2Int startCoord, Vector2Int endCoord,
        GridManager gridManager, IEnumerable<UnitBase> allUnits)
    {
        List<UnitBase> attackers = new List<UnitBase>();

        if (mover == null || gridManager == null || allUnits == null)
            return attackers;

        foreach (var unit in allUnits)
        {
            if (unit == null || unit == mover)
                continue;

            if (unit.Faction == mover.Faction)
                continue; // 같은 세력은 ZOC를 투사하지 않음

            if (!ThreatensZoneOfControl(unit))
                continue;

            int distBefore = gridManager.GetDistance(startCoord, unit.GridCoord);
            int distAfter = gridManager.GetDistance(endCoord, unit.GridCoord);

            bool wasAdjacent = distBefore <= unit.AttackRange;
            bool stillAdjacent = distAfter <= unit.AttackRange;

            if (wasAdjacent && !stillAdjacent)
                attackers.Add(unit);
        }

        return attackers;
    }

    // 근접 사거리(AttackRange <= 1)를 가진, 기절하지 않은, 재장전이 필요한데 안 된 상태가 아닌 유닛만
    // ZOC를 투사한다. PerformCounterattack이 쓰는 가드 조건과 동일하게 맞춤.
    private static bool ThreatensZoneOfControl(UnitBase unit)
    {
        if (unit.AttackRange > 1)
            return false; // 원거리 무기는 ZOC 없음

        if (unit.IsStunned)
            return false;

        if (unit.MainHandWeapon != null && unit.MainHandWeapon.requiresReload && !unit.IsLoaded)
            return false;

        return true;
    }
}