using UnityEngine;

/// <summary>
/// 종족이 가질 수 있는 고유 특성의 기반 클래스.
/// 지금 필요한 세 지점(이동 비용/최대 공격 횟수/근접 힘)에만 개입하도록 최소한의 훅만 둔다.
/// 나중에 새로운 종류의 특성이 필요해지면, 여기에 훅을 하나 더 추가하고
/// 그걸 구현하는 새 구체 클래스를 만들면 된다 (기존 특성들은 안 건드려도 됨).
/// </summary>
public abstract class RacialTrait : ScriptableObject
{
    public string traitName;
    [TextArea] public string description;

    public virtual int ModifyMoveCost(TileInstance tile, int baseCost) => baseCost;
    public virtual int ModifyMaxAttacks(int baseMaxAttacks) => baseMaxAttacks;
    public virtual int ModifyMeleeStrength(int baseStrength) => baseStrength;
}