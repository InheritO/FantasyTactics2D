using UnityEngine;

[CreateAssetMenu(fileName = "NewWeaponAttack", menuName = "Strategy/Equipment/Weapon Attack")]
public class WeaponAttack : ScriptableObject
{
    public string attackName;

    [Header("Bonus (무기 기본값에 더해짐)")]
    [Tooltip("무기의 기본 위력에 더해지는 보너스")]
    public int powerBonus;

    [Tooltip("무기의 기본 관통력에 더해지는 보너스")]
    public int armorPenetrationBonus;

    [Tooltip("무기의 기본 명중 보정에 더해지는 보너스")]
    public int accuracyBonusModifier;

    [Tooltip("무기의 기본 치명타 성향에 더해지는 보너스")]
    public int critRatingBonus;

    [Header("Status Effect (선택 사항)")]
    public StatusEffectType inflictedEffect = StatusEffectType.None;
    public int disruption; // 상태이상 적중 판정에 쓰이는 값 (대상 맷집과 대결)
    public int effectDuration = 1; // 몇 턴 지속되는지
    public int effectMagnitude; // 출혈의 턴당 데미지량 등, 효과 종류에 따라 의미가 다름
}