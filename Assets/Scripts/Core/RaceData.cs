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

    [Header("Base Stats")]
    public int maxHealth = 10;
    public int baseMoveRange = 3;
    public int baseMeleeSkill = 3;
    public int baseRangedSkill = 3;
    public int baseStrength = 3;
    public int baseConstitution = 3;
    public int baseAgility = 3;

    [Header("Roster Cost")]
    public int unitCost = 10; // 대전 편성 시 이 종족 유닛 한 기의 기본 비용

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