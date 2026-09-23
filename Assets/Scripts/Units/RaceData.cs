using UnityEngine;

[System.Serializable]
public struct WeaponCostOverride
{
    public WeaponData weapon;
    public int costModifier;
}

[System.Serializable]
public struct ArmorCostOverride
{
    public ArmorData armor;
    public int costModifier;
}

[System.Serializable]
public struct ShieldCostOverride
{
    public ShieldData shield;
    public int costModifier;
}

[CreateAssetMenu(fileName = "NewRace", menuName = "Strategy/Race")]
public class RaceData : ScriptableObject
{
    public string raceName;

    [Header("Visuals")]
    public CharacterAnimationSet visualSet;      // 몸
    public CharacterAnimationSet headVisualSet;  // 머리 (머리카락/모자 포함)

    [Tooltip("갑옷을 장착하지 않았을 때 기본으로 쓸 ArmorData (맨몸 상태)")]
    public ArmorData unarmoredArmor;

    [Tooltip("체형 크기 배율. 1 = 기본 크기, 드워프처럼 작게 하려면 0.8~0.9 정도")]
    public float visualScale = 1f;

    [Header("Base Stats")]
    [Tooltip("최대 체력")]
    public int maxHealth = 10;
    [Tooltip("한 턴에 이동 가능한 칸 수")]
    public int baseMoveRange = 20;
    [Tooltip("근접 공격 시 막기 무력화 판정에 사용 (공격자 근접기술 vs 방어자 방어기술)")]
    public int baseMeleeSkill = 20;
    [Tooltip("원거리 공격 시 막기 무력화 판정에 사용 (공격자 원거리기술 vs 방어자 방어기술)")]
    public int baseRangedSkill = 20;
    [Tooltip("방패를 착용했을 때 막기 성공률에 사용 (방어자 방어기술 vs 공격자 근접/원거리기술)")]
    public int baseDefenseSkill = 20;
    [Tooltip("힘 기반 무기의 데미지에 영향")]
    public int baseStrength = 20;
    [Tooltip("받는 데미지 감소(방어구로 무시되지 않음), 상태이상 저항 판정에도 사용")]
    public int baseConstitution = 20;
    [Tooltip("명중/회피 판정에 사용 (공격자 민첩 vs 방어자 민첩)")]
    public int baseAgility = 20;

    [Header("Roster Cost")]
    public int unitCost = 10; // 대전 편성 시 이 종족 유닛 한 기의 기본 비용

    [Header("Racial Traits")]
    public RacialTrait[] traits = new RacialTrait[0];

    [Header("Available Equipment")]
    public WeaponData[] availableWeapons = new WeaponData[0];
    public ArmorData[] availableArmors = new ArmorData[0];
    public ShieldData[] availableShields = new ShieldData[0];

    [Header("Cost Overrides (기본 비용에 가산)")]
    public WeaponCostOverride[] weaponCostOverrides = new WeaponCostOverride[0];
    public ArmorCostOverride[] armorCostOverrides = new ArmorCostOverride[0];
    public ShieldCostOverride[] shieldCostOverrides = new ShieldCostOverride[0];


    // 이 종족이 특정 무기를 장착할 때의 실제 비용 (예외표에 없으면 무기 자체의 기본 비용)
    public int GetWeaponCost(WeaponData weapon)
    {
        if (weapon == null)
            return 0;

        int cost = weapon.cost;

        if (weaponCostOverrides != null)
        {
            foreach (var entry in weaponCostOverrides)
            {
                if (entry.weapon == weapon)
                {
                    cost += entry.costModifier;
                    break;
                }
                    
            }
        }

        return Mathf.Max(0, cost);
    }

    public int GetArmorCost(ArmorData armor)
    {
        if (armor == null)
            return 0;

        int cost = armor.cost;

        if (armorCostOverrides != null)
        {
            foreach (var entry in armorCostOverrides)
            {
                if (entry.armor == armor)
                {
                    cost += entry.costModifier;
                    break;
                }
            }
        }

        return Mathf.Max(0, cost);
    }

    public int GetShieldCost(ShieldData shield)
    {
        if (shield == null)
            return 0;

        int cost = shield.cost;

        if (shieldCostOverrides != null)
        {
            foreach (var entry in shieldCostOverrides)
            {
                if (entry.shield == shield)
                {
                    cost += entry.costModifier;
                    break;
                }
            }
        }

        return Mathf.Max(0, cost);
    }
}