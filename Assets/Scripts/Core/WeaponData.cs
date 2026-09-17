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

[System.Flags]
public enum WeaponSlotType
{
    None = 0,
    MainHand = 1 << 0,   // 검, 창, 활, 대검 등 — 주손에만 장착 가능
    OffHand = 1 << 1,  // 단검처럼 가볍고 보조 슬롯에도 들어갈 수 있는 무기
}

[CreateAssetMenu(fileName = "NewWeapon", menuName = "Strategy/Equipment/Weapon")]
public class WeaponData : ScriptableObject
{
    public string weaponName;
    public Sprite icon;


    [Header("Handedness")]
    public WeaponHandedness handedness = WeaponHandedness.OneHanded;
    public WeaponSlotType slotType = WeaponSlotType.MainHand;

    [Header("Range")]
    public bool isRanged;
    public int attackRangeOverride = -1; // -1이면 유닛 기본 사거리 유지

    [Header("Base Combat Stats")]

    [Tooltip("이 공격의 기본 위력. 힘 기반 무기는 여기에 캐릭터 힘이 더해짐")]
    public int basePower;
    public DamageScaling damageScaling = DamageScaling.Strength;

    [Tooltip("무기 자체의 기본 관통력")]
    public int baseArmorPenetration;

    [Tooltip("무기 자체의 기본 명중 보정")]
    public int baseAccuracyBonus;

    [Tooltip("무기 자체의 기본 치명타 성향. 치명타 판정에 사용되는 값(방어자 맷집과 대결)")]
    public int baseCritRating = 10;

    [Header("Attacks (최소 1개 이상)")]
    public WeaponAttack[] attacks = new WeaponAttack[1];

    [Header("Display")]
    [TextArea] public string description;

    [Header("Roster Cost")]
    public int cost = 2; // 기본 비용. 종족별로 다르게 하려면 RaceData의 weaponCostOverrides에 등록


    public WeaponAttack GetDefaultAttack()
    {
        return (attacks != null && attacks.Length > 0) ? attacks[0] : null;
    }
}