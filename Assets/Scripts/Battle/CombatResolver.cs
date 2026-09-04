using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 두 유닛 간 전투 판정을 계산한다. 상태를 갖지 않는 순수 계산 클래스.
/// MovementRangeCalculator와 같은 패턴: 계산만 하고, 적용은 호출한 쪽이 담당한다.
/// </summary>
public static class CombatResolver
{
    private const int BaseHitChance = 70; // 기술과 회피가 같을 때의 기본 명중률(%)

    public static List<CombatResult> ResolveFullAttack(UnitBase attacker, UnitBase defender)
    {
        List<CombatResult> results = new List<CombatResult>();

        CombatResult mainResult = Resolve(attacker, defender, attacker.MainHandWeapon);
        results.Add(mainResult);

        foreach (var ability in attacker.GetActiveAbilities())
        {
            CombatResult? extra = ability.TryTrigger(attacker, defender);
            if (extra.HasValue)
                results.Add(extra.Value);
        }

        return results;
    }

    public static CombatResult Resolve(UnitBase attacker, UnitBase defender, WeaponData weapon)
    {
        int hitChance = CalculateHitChance(attacker, defender, weapon);
        bool isHit = Random.Range(0, 100) < hitChance;

        if (!isHit)
            return CombatResult.Miss();

        int damage = CalculateDamage(attacker, defender, weapon);
        return CombatResult.Hit(damage);
    }

    public static int CalculateHitChance(UnitBase attacker, UnitBase defender, WeaponData weapon)
    {
        int attackSkill = (weapon != null && weapon.isRanged) ? attacker.RangedSkill : attacker.MeleeSkill;
        int accuracyBonus = weapon?.accuracyBonus ?? 0;


        int chance = BaseHitChance + (attackSkill - defender.Agility) * 5 + accuracyBonus; // 기술-회피 차이 1당 5%p 조정
        return Mathf.Clamp(chance, 5, 95); // 완전 100%/0%는 지양 (항상 약간의 운 개입)
    }

    public static int CalculateDamage(UnitBase attacker, UnitBase defender, WeaponData weapon)
    {
        int rawDamage = weapon == null
            ? attacker.Strength // 비무장은 맨손 데미지(힘 기반)
            : (weapon.damageScaling == DamageScaling.Strength
                ? weapon.basePower + attacker.Strength
                : weapon.basePower);

        int armorPenetration = weapon?.armorPenetration ?? 0;
        int effectiveArmorDefense = Mathf.Max(0, defender.ArmorDefense - armorPenetration);
        int effectiveDefense = defender.ConstitutionDefense + effectiveArmorDefense;

        int finalDamage = rawDamage - effectiveDefense;
        return Mathf.Max(1, finalDamage); // 최소 1 데미지 보장
    }
}