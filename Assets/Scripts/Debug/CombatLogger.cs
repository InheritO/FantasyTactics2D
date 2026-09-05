using UnityEngine;

/// <summary>
/// 전투 관련 이벤트를 구독해서 콘솔에 로그로 출력하는 디버그 도구.
/// 실제 UI/사운드/이펙트가 만들어지면 이 클래스는 참고만 하고 대체될 수 있음.
/// </summary>
public class CombatLogger : MonoBehaviour
{
    public void RegisterUnit(UnitBase unit)
    {
        unit.OnMoved += HandleMoved;
        unit.OnAttackPerformed += HandleAttackPerformed;
        unit.OnAttackResult += HandleAttackResult;
        unit.OnDamaged += HandleDamaged;
        unit.OnDied += HandleDied;
        unit.OnActionsExhausted += HandleActionsExhausted;
    }

    private void HandleMoved(UnitBase unit, Vector2Int from, Vector2Int to)
    {
        Debug.Log($"[{unit.name}] 이동: {from} → {to}");
    }

    private void HandleAttackPerformed(UnitBase attacker, UnitBase target)
    {
        Debug.Log($"[{attacker.name}]이(가) {target.name}을(를) 공격합니다.");
    }

    private void HandleAttackResult(UnitBase attacker, UnitBase target, CombatResult result)
    {
        if (result.IsHit)
            Debug.Log($"  → 명중! {result.DamageDealt} 데미지.");
        else
            Debug.Log($"  → 빗나감.");
    }

    private void HandleDamaged(UnitBase unit, int amount)
    {
        Debug.Log($"[{unit.name}] {amount} 데미지 받음. 남은 체력: {unit.CurrentHealth}/{unit.MaxHealth}");
    }

    private void HandleDied(UnitBase unit)
    {
        Debug.Log($"[{unit.name}] 사망.");
    }

    private void HandleActionsExhausted(UnitBase unit)
    {
        Debug.Log($"[{unit.name}] 이번 턴에 더 이상 행동할 수 없습니다.");
    }
}