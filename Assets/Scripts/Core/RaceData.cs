using UnityEngine;

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

    [Header("Available Equipment")]
    public WeaponData[] availableWeapons;
    public ArmorData[] availableArmors;
}