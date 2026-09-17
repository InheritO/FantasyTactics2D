using UnityEngine;

/// <summary>
/// 근접 공격(비무장 포함) 시 힘 스탯에 보너스를 준다. 원거리 무기 사용 시엔 적용되지 않는다.
/// </summary>
[CreateAssetMenu(fileName = "MeleeStrengthBonus", menuName = "Strategy/Traits/Melee Strength Bonus")]
public class MeleeStrengthBonusTrait : RacialTrait
{
    public int bonus = 1;

    public override int ModifyMeleeStrength(int baseStrength) => baseStrength + bonus;
}