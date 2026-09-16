/// <summary>
/// 유닛에게 실제로 걸려있는 상태이상 하나. 순수 데이터 클래스 (SO 아님, 유닛마다 독립적인 상태이므로).
/// </summary>
public class StatusEffectInstance
{
    public StatusEffectType Type { get; private set; }
    public int RemainingTurns { get; private set; }
    public int Magnitude { get; private set; } // 출혈이면 "턴당 데미지량"으로 사용. 기절은 현재 미사용.

    public StatusEffectInstance(StatusEffectType type, int duration, int magnitude)
    {
        Type = type;
        RemainingTurns = duration;
        Magnitude = magnitude;
    }

    // 턴이 지날 때마다 호출. 지속시간이 0 이하가 되면 만료된 것으로 간주.
    public void DecrementTurn()
    {
        RemainingTurns--;
    }

    public bool IsExpired => RemainingTurns < 0;
}