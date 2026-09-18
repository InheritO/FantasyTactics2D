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

    [Tooltip("무기 자체의 기본 치명타 성향. 치명타 판정에 사용되는 확률")]
    public int baseCritRating = 10;

    [Header("Attacks (최소 1개 이상)")]
    public WeaponAttack[] attacks = new WeaponAttack[1];

    [Header("Display")]
    [TextArea] public string description;

    [Header("Roster Cost")]
    public int cost = 2; // 기본 비용. 종족별로 다르게 하려면 RaceData의 weaponCostOverrides에 등록

    [Header("Reload (선택 사항)")]
    [Tooltip("체크하면, 공격 후 다음 턴엔 재장전 행동만 가능해짐")]
    public bool requiresReload;

    [Tooltip("재장전 시 사용할 행동 에셋. 보통 공용 ReloadAction 에셋 하나를 여러 무기가 공유")]
    public ReloadAction reloadAction;

    [Header("Counterattack (선택 사항)")]
    [Tooltip("체크하면, 이 무기를 든 상태로 근접 사거리 안에서 공격받았을 때 자동으로 반격한다. " +
    "방어태세(Steady)와는 무관하게 상시 적용됨 — 방어태세는 막기 확률에만 관여한다.")]
    public bool grantsCounterattack = false;

    public WeaponAttack GetDefaultAttack()
    {
        return (attacks != null && attacks.Length > 0) ? attacks[0] : null;
    }
}