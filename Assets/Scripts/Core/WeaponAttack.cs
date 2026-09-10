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
}