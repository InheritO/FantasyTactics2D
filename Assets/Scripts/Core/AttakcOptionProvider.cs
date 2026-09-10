using System.Collections.Generic;

/// <summary>
/// 선택된 유닛이 대상에게 실행 가능한 공격 옵션 목록을 만든다.
/// 주무기가 가진 모든 WeaponAttack을 옵션으로 나열한다.
/// </summary>
public static class AttackOptionProvider
{
    public static List<AttackOption> GetOptions(UnitBase attacker, UnitBase target)
    {
        var options = new List<AttackOption>();

        if (attacker == null || target == null)
            return options;

        WeaponData weapon = attacker.MainHandWeapon;

        if (weapon == null || weapon.attacks == null || weapon.attacks.Length == 0)
        {
            // 비무장이거나 공격 방식이 없는 무기는 기본 공격(맨손) 하나만 제공
            options.Add(new AttackOption
            {
                Label = "공격",
                Execute = () => attacker.TryAttack(target)
            });
            return options;
        }

        foreach (var attack in weapon.attacks)
        {
            if (attack == null)
                continue;

            WeaponAttack chosenAttack = attack; // 클로저 캡처 문제 방지용 지역 변수

            options.Add(new AttackOption
            {
                Label = attack.attackName,
                Execute = () => attacker.TryAttack(target, chosenAttack)
            });
        }

        return options;
    }
}