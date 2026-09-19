using UnityEngine;

/// <summary>
/// 보조무기(off-hand)로 인한 추가 공격. 쌍검처럼 두 번째 무기를 든 경우 발동한다.
/// 원래 공격보다는 약화된 확률로 적용한다 (accuracyMultiplier로 CombatResolver.Resolve에 전달).
/// </summary>
public class ExtraAttackAbility : IWeaponAbility
{
    private readonly WeaponData offHandWeapon;
    private readonly float accuracyMultiplier = 0.75f;

    public ExtraAttackAbility(WeaponData offHandWeapon, float accuracyMultiplier = 0.8f)
    {
        this.offHandWeapon = offHandWeapon;
        this.accuracyMultiplier = accuracyMultiplier;
    }

    public CombatResult? TryTrigger(UnitBase attacker, UnitBase defender)
    {
        WeaponAttack attack = offHandWeapon.GetDefaultAttack();
        return CombatResolver.Resolve(attacker, defender, offHandWeapon, attack, accuracyMultiplier);
    }
}