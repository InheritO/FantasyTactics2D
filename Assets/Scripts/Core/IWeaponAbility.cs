/// <summary>
/// 무기가 가진 공격 관련 특수 효과. 하나의 무기는 여러 개를 가질 수 있다.
/// CombatResolver가 공격을 계산하는 과정에서 이 어빌리티들을 순서대로 적용한다.
/// </summary>
public interface IWeaponAbility
{
    // 공격이 발생할 때마다 호출됨. attacker/defender를 보고 추가 CombatResult를 만들어 반환할 수 있음
    // 아무 추가 효과가 없으면 null 반환
    CombatResult? TryTrigger(UnitBase attacker, UnitBase defender);
}