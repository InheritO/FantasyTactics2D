
/// <summary>
/// 한 번의 공격 판정 결과. 실제 적용 전, 계산된 정보만 담는다.
/// </summary>
public struct CombatResult
{
    public bool IsHit;
    public int DamageDealt;

    public static CombatResult Miss() => new CombatResult { IsHit = false, DamageDealt = 0 };
    public static CombatResult Hit(int damage) => new CombatResult { IsHit = true, DamageDealt = damage };
}
