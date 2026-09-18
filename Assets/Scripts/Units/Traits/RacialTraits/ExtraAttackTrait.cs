using UnityEngine;

/// <summary>
/// 턴당 최대 공격 횟수를 늘려준다.
/// </summary>
[CreateAssetMenu(fileName = "ExtraAttack", menuName = "Strategy/Traits/Extra Attack")]
public class ExtraAttackTrait : RacialTrait
{
    public int bonusActions = 1;

    public override int ModifyMaxActions(int baseMaxAttacks) => baseMaxAttacks + bonusActions;
}