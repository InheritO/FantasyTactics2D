using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 두 유닛 간 전투 판정을 계산한다. 상태를 갖지 않는 순수 계산 클래스.
/// MovementRangeCalculator와 같은 패턴: 계산만 하고, 적용은 호출한 쪽이 담당한다.
/// </summary>
public static class CombatResolver
{

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

        // 1단계: 명중/회피 (민첩 vs 민첩)
        int hitChance = CalculateHitChance(attacker, defender, weapon, attack);
        bool isHit = Random.Range(0, 100) < hitChance;

        if (!isHit)
            return CombatResult.Miss();

        // 2단계: 막기 (공격 기술 vs 방어 기술, 방패 있을 때만)
        if (TryResolveBlock(attacker, defender, weapon))
        {
            Debug.Log($"[{defender.name}] 방패로 막아냈습니다!");
            return CombatResult.Blocked();
        }

        // 3단계: 데미지 (힘/무기 vs 맷집/방어구)
        int damage = CalculateDamage(attacker, defender, weapon, attack);

        // 명중했을 때만 상태이상 판정 시도
        if (attack != null && attack.inflictedEffect != StatusEffectType.None)
        {
            bool effectLands = TryResolveStatusEffect(attack, defender);

            if (effectLands)
                return CombatResult.HitWithEffect(damage, attack.inflictedEffect);
        }

        return CombatResult.Hit(damage);
    }

    public static int CalculateHitChance(UnitBase attacker, UnitBase defender, WeaponData weapon, WeaponAttack attack)
    {
        //공격자와 방어자의 순발력을 비교
        int totalAccuracyBonus = (weapon?.baseAccuracyBonus ?? 0) + (attack?.accuracyBonusModifier ?? 0);

        int chance = 70 + (attacker.Agility - defender.Agility) * 5 + totalAccuracyBonus;
        return Mathf.Clamp(chance, 5, 95);
    }

    public static int CalculateDamage(UnitBase attacker, UnitBase defender, WeaponData weapon, WeaponAttack attack)
    {
        // 원거리 무기가 아니면(비무장 포함) 근접으로 취급 -> 근접 특성(오크 등)이 여기서 적용됨
        bool isMelee = weapon == null || !weapon.isRanged;
        int effectiveStrength = attacker.GetEffectiveStrength(isMelee);


        int totalPower = (weapon?.basePower ?? 0) + (attack?.powerBonus ?? 0);
        int rawDamage = (weapon != null && weapon.damageScaling == DamageScaling.Strength)
        ? totalPower + effectiveStrength
        : totalPower;

        rawDamage = Mathf.Max(0, rawDamage);

        int armorPenetration = Mathf.Max(0, (weapon?.baseArmorPenetration ?? 0) + (attack?.armorPenetrationBonus ?? 0));

        int effectiveArmorDefense = Mathf.Max(0, defender.ArmorDefense - armorPenetration);
        int effectiveDefense = defender.ConstitutionDefense + effectiveArmorDefense;

        int finalDamage = rawDamage - effectiveDefense;
        return Mathf.Max(1, finalDamage);
    }

    private static bool TryResolveBlock(UnitBase attacker, UnitBase defender, WeaponData attackerWeapon)
    {
        if (defender.EquippedShield == null || defender.ShieldBroken)
            return false;

        // 막기는 공격자의 무기 숙련도(근접/원거리)와, 방어자의 방어 기술이 대결
        bool isRangedAttack = attackerWeapon != null && attackerWeapon.isRanged;
        int attackerSkill = isRangedAttack ? attacker.RangedSkill : attacker.MeleeSkill;

        int totalBlockSkill = defender.DefenseSkill + defender.EquippedShield.blockSkillBonus;

        int blockChance = 20 + (totalBlockSkill - attackerSkill) * 2;
        blockChance = Mathf.Clamp(blockChance, 5, 60);

        return Random.Range(0, 100) < blockChance;
    }

    // 상태이상 적중 여부만 판정 (별도 함수로 분리해서, 나중에 UI 등에서 "적중 확률 미리보기"로도 재사용 가능하게)
    private static bool TryResolveStatusEffect(WeaponAttack attack, UnitBase defender)
    {
        int chance = 40 + (attack.disruption - defender.ConstitutionDefense) * 2;
        chance = Mathf.Clamp(chance, 5, 80);

        return Random.Range(0, 100) < chance;
    }
}