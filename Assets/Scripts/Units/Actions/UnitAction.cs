using UnityEngine;

/// <summary>
/// 유닛이 자기 턴에 선택할 수 있는 행동 하나를 나타내는 추상 클래스.
/// 공격(WeaponAttack)뿐 아니라 재장전, 방어태세 등 "공격이 아닌 행동"도 이 아래에 속한다.
/// </summary>
public abstract class UnitAction : ScriptableObject
{
    public string actionName;

    // 지금 상황에서 이 행동을 선택할 수 있는지
    public abstract bool IsAvailable(UnitBase actor, UnitBase target);

    // 실행. target은 공격류에만 의미 있고, 재장전 등은 null일 수 있음
    public abstract void Execute(UnitBase actor, UnitBase target);
}