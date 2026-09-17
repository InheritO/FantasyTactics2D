
/// <summary>
/// 한 번의 공격 판정 결과. 실제 적용 전, 계산된 정보만 담는다.
/// </summary>
public struct CombatResult
{
    public bool IsHit;
    public bool IsBlocked;
    public bool IsCritical;
    public int DamageDealt;
    public StatusEffectType InflictedEffect;

    public static CombatResult Miss() => new CombatResult { IsHit = false, DamageDealt = 0 };
    public static CombatResult Blocked() =>
       new CombatResult { IsHit = true, IsBlocked = true, DamageDealt = 0, InflictedEffect = StatusEffectType.None };

    public static CombatResult Hit(int damage, bool isCritical = false) =>
        new CombatResult { IsHit = true, IsBlocked = false, IsCritical = isCritical, DamageDealt = damage, InflictedEffect = StatusEffectType.None };

    public static CombatResult HitWithEffect(int damage, StatusEffectType effect, bool isCritical = false) =>
        new CombatResult { IsHit = true, IsBlocked = false, IsCritical = isCritical, DamageDealt = damage, InflictedEffect = effect };
}

