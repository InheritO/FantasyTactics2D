using System;
using System.Collections.Generic;

/// <summary>
/// 유닛에게 걸린 상태이상(출혈/기절 등) 목록을 관리한다.
/// 순수 로직 클래스(MonoBehaviour 아님). UnitBase가 이 클래스를 소유하고,
/// 턴 시작 시점에 처리를 위임한다.
/// </summary>
public class UnitStatusEffectTracker
{
    private List<StatusEffectInstance> activeEffects = new List<StatusEffectInstance>();

    // 기절 상태인지 여부 (이동/공격 가능 여부 판정에 사용)
    public bool IsStunned => activeEffects.Exists(e => e.Type == StatusEffectType.Stun);

    public void Apply(StatusEffectType type, int duration, int magnitude)
    {
        /* 같은 종류의 상태이상이 이미 걸려있으면, 지속시간은 항상 최신 값으로 갱신하되
          데미지(magnitude)는 기존값과 신규값 중 더 강한 쪽을 유지한다.
          (약한 무기로 다시 때렸다고 기존 강한 효과가 약해지는 건 부자연스럽고,
          반대로 완전 중첩을 허용하면 데미지가 무한히 누적될 위험이 있어 절충함)*/
        StatusEffectInstance existing = activeEffects.Find(e => e.Type == type);

        if (existing != null)
        {
            int strongerMagnitude = Math.Max(existing.Magnitude, magnitude);
            activeEffects.Remove(existing);
            activeEffects.Add(new StatusEffectInstance(type, duration, strongerMagnitude));
        }
        else
        {
            activeEffects.Add(new StatusEffectInstance(type, duration, magnitude));
        }
    }

    /* 턴 시작 시 호출: 출혈 데미지 적용, 지속시간 감소, 만료된 효과 제거
     출혈 데미지는 UnitBase.TakeDamage를 직접 호출하지 않고 콜백으로 위임한다
     (이 클래스가 UnitBase에 대한 참조를 몰라도 되도록 분리하기 위함)*/
    public void ProcessTurnStart(Action<int> onBleedDamage)
    {
        foreach (var effect in activeEffects)
        {
            if (effect.Type == StatusEffectType.Bleed)
                onBleedDamage?.Invoke(effect.Magnitude);

            effect.DecrementTurn();
        }

        activeEffects.RemoveAll(e => e.IsExpired);
    }
}