using UnityEngine;

[System.Serializable]
public struct WeaponCostOverride
{
    public WeaponData weapon;
    public int cost;
}

[System.Serializable]
public struct ArmorCostOverride
{
    public ArmorData armor;
    public int cost;
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
    public WeaponData[] availableWeapons;
    public ArmorData[] availableArmors;
    public ShieldData[] availableShields;

    [Header("Cost Overrides (미등록 장비는 장비 자체의 기본 cost 사용)")]
    public WeaponCostOverride[] weaponCostOverrides;
    public ArmorCostOverride[] armorCostOverrides;


    // 이 종족이 특정 무기를 장착할 때의 실제 비용 (예외표에 없으면 무기 자체의 기본 비용)
    public int GetWeaponCost(WeaponData weapon)
    {
        if (weapon == null)
            return 0;

        if (weaponCostOverrides != null)
        {
            foreach (var entry in weaponCostOverrides)
            {
                if (entry.weapon == weapon)
                    return entry.cost;
            }
        }

        return weapon.cost;
    }

    public int GetArmorCost(ArmorData armor)
    {
        if (armor == null)
            return 0;

        if (armorCostOverrides != null)
        {
            foreach (var entry in armorCostOverrides)
            {
                if (entry.armor == armor)
                    return entry.cost;
            }
        }

        return armor.cost;
    }
}