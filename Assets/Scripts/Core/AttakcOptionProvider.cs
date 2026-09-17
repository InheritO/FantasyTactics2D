using System.Collections.Generic;

/// <summary>
/// 선택된 유닛이 지금 실행 가능한 행동(UnitAction) 목록을 만든다.
/// 무기가 가진 공격들과, 재장전 같은 특수 행동이 모두 같은 방식으로 취급된다.
/// </summary>
public static class AttackOptionProvider
{
    public static List<AttackOption> GetOptions(UnitBase attacker, UnitBase target)
    {
        var options = new List<AttackOption>();

        if (attacker == null)
            return options;

        List<UnitAction> candidateActions = new List<UnitAction>();

        WeaponData weapon = attacker.MainHandWeapon;

        if (weapon != null && weapon.attacks != null)
        {
            foreach (var attack in weapon.attacks)
                if (attack != null)
                    candidateActions.Add(attack);
        }

        if (weapon != null && weapon.requiresReload && weapon.reloadAction != null)
            candidateActions.Add(weapon.reloadAction);

        foreach (var action in candidateActions)
        {
            if (!action.IsAvailable(attacker, target))
                continue;

            UnitAction chosenAction = action;

            options.Add(new AttackOption
            {
                Label = chosenAction.actionName,
                Execute = () => chosenAction.Execute(attacker, target)
            });
        }

        if (options.Count == 0 && target != null && (weapon == null || weapon.attacks == null || weapon.attacks.Length == 0))
        {
            options.Add(new AttackOption
            {
                Label = "공격",
                Execute = () => attacker.TryAttack(target)
            });
        }

        return options;
    }
}