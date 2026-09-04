using UnityEngine;

/// <summary>
/// 보조무기(off-hand)로 인한 추가 공격. 쌍검처럼 두 번째 무기를 든 경우 발동한다.
/// 원래 공격보다는 약화된 확률/위력으로 적용한다.
/// </summary>
public class ExtraAttackAbility : IWeaponAbility
{
    private readonly WeaponData offHandWeapon;
    private readonly float accuracyMultiplier;

    public ExtraAttackAbility(WeaponData offHandWeapon, float accuracyMultiplier = 0.7f)
    {
        this.offHandWeapon = offHandWeapon;
        this.accuracyMultiplier = accuracyMultiplier;
    }

    public CombatResult? TryTrigger(UnitBase attacker, UnitBase defender)
    {
        // 보조무기 전용 명중 판정 (기본 명중률에 페널티 적용)
        int baseChance = CombatResolver.CalculateHitChance(attacker, defender, offHandWeapon);
        int adjustedChance = Mathf.RoundToInt(baseChance * accuracyMultiplier);

        bool isHit = Random.Range(0, 100) < adjustedChance;
        if (!isHit)
            return CombatResult.Miss();

        int damage = CombatResolver.CalculateDamage(attacker, defender, offHandWeapon);
        return CombatResult.Hit(damage);
    }
}