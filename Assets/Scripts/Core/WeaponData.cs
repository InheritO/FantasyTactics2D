using UnityEngine;

public enum DamageScaling
{
    Strength,   // 힘에 비례 (검, 창, 활, 투척무기)
    Fixed       // 힘과 무관, 무기 자체 위력이 곧 데미지 (석궁, 총)
}

public enum WeaponHandedness
{
    OneHanded, // 한 손 무기 (손 1개 차지)
    TwoHanded  // 양손 무기 (손 2개 차지)
}

public enum WeaponSlotType
{
    MainHandOnly,   // 검, 창, 활, 대검 등 — 주손에만 장착 가능
    OffHandCapable  // 단검처럼 가볍고 보조 슬롯에도 들어갈 수 있는 무기
}

[CreateAssetMenu(fileName = "NewWeapon", menuName = "Strategy/Equipment/Weapon")]
public class WeaponData : ScriptableObject
{
    public string weaponName;
    public Sprite icon;


    [Header("Handedness")]
    public WeaponHandedness handedness = WeaponHandedness.OneHanded;
    public WeaponSlotType slotType = WeaponSlotType.MainHandOnly;

    [Header("Range")]
    public bool isRanged;
    public int attackRangeOverride = -1; // -1이면 유닛 기본 사거리 유지

    [Header("Damage")]
    public int basePower;
    public DamageScaling damageScaling = DamageScaling.Strength;

    [Header("Armor Interaction")]
    public int armorPenetration; // 상대 방어구 보너스를 깎는 수치 (맷집에는 영향 없음)

    [Header("Accuracy")]
    public int accuracyBonus; // 명중률 보정 (기계식 무기 등에 유용)

    [Header("Display")]
    [TextArea] public string description;

}