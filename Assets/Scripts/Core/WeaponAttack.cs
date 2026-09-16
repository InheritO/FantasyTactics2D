using UnityEngine;

[CreateAssetMenu(fileName = "NewWeaponAttack", menuName = "Strategy/Equipment/Weapon Attack")]
public class WeaponAttack : ScriptableObject
{
    public string attackName;

    [Header("Damage")]
    public int basePower;

    [Header("Armor Interaction")]
    public int armorPenetration;  // 상대 방어구 보너스를 깎는 수치 (맷집에는 영향 없음)

    [Header("Accuracy")]
    public int accuracyBonus; // 명중률 보정 (기계식 무기 등에 유용)

    [Header("Status Effect (선택 사항)")]
    public StatusEffectType inflictedEffect = StatusEffectType.None;
    public int disruption; // 상태이상 적중 판정에 쓰이는 값 (대상 맷집과 대결)
    public int effectDuration = 1; // 몇 턴 지속되는지
    public int effectMagnitude; // 출혈의 턴당 데미지량 등, 효과 종류에 따라 의미가 다름
}