using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 두 유닛 간 전투 판정을 계산한다. 상태를 갖지 않는 순수 계산 클래스.
/// MovementRangeCalculator와 같은 패턴: 계산만 하고, 적용은 호출한 쪽이 담당한다.
/// </summary>
public static class CombatResolver
{
    private const int BaseHitChance = 70; // 기술과 회피가 같을 때의 기본 명중률(%)

    public static List<CombatResult> ResolveFullAttack(UnitBase attacker, UnitBase defender, WeaponAttack chosenAttack)
    {
        List<CombatResult> results = new List<CombatResult>();

        if (attacker == null || defender == null)
        {
            Debug.LogWarning("ResolveFullAttack에 null 유닛이 전달되었습니다.");
            return results;
        }

        CombatResult mainResult = Resolve(attacker, defender, attacker.MainHandWeapon, chosenAttack);
        results.Add(mainResult);

        foreach (var ability in attacker.GetActiveAbilities())
        {
            if (defender == null || defender.CurrentHealth <= 0)
                break;

            CombatResult? extra = ability.TryTrigger(attacker, defender);
            if (extra.HasValue)
                results.Add(extra.Value);
        }

        return results;
    }

    public static CombatResult Resolve(UnitBase attacker, UnitBase defender, WeaponData weapon, WeaponAttack attack)
    {
        if (attacker == null || defender == null)
        {
            Debug.LogWarning("Resolve에 null 유닛이 전달되었습니다.");
            return CombatResult.Miss();
        }

        int hitChance = CalculateHitChance(attacker, defender, weapon, attack);
        bool isHit = Random.Range(0, 100) < hitChance;

        if (!isHit)
            return CombatResult.Miss();

        int damage = CalculateDamage(attacker, defender, weapon, attack);
        return CombatResult.Hit(damage);
    }

    public static int CalculateHitChance(UnitBase attacker, UnitBase defender, WeaponData weapon, WeaponAttack attack)
    {
        int attackSkill = (weapon != null && weapon.isRanged) ? attacker.RangedSkill : attacker.MeleeSkill;
        int accuracyBonus = attack?.accuracyBonus ?? 0;

        int chance = BaseHitChance + (attackSkill - defender.Agility) * 5 + accuracyBonus;
        return Mathf.Clamp(chance, 5, 95);
    }

    public static int CalculateDamage(UnitBase attacker, UnitBase defender, WeaponData weapon, WeaponAttack attack)
    {
        int rawDamage = attack == null
            ? attacker.Strength
            : (weapon.damageScaling == DamageScaling.Strength
                ? attack.basePower + attacker.Strength
                : attack.basePower);

        rawDamage = Mathf.Max(0, rawDamage);

        int armorPenetration = Mathf.Max(0, attack?.armorPenetration ?? 0);
        int effectiveArmorDefense = Mathf.Max(0, defender.ArmorDefense - armorPenetration);
        int effectiveDefense = defender.ConstitutionDefense + effectiveArmorDefense;

        int finalDamage = rawDamage - effectiveDefense;
        return Mathf.Max(1, finalDamage);
    }


}