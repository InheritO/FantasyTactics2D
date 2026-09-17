using UnityEngine;
using NaughtyAttributes;

[CreateAssetMenu(fileName = "NewArmor", menuName = "Strategy/Equipment/Armor")]
public class ArmorData : ScriptableObject
{
    public string armorName;
    public Sprite icon;

    [Header("Stat Modifiers")]
    public int defenseBonus;
    public int moveRangePenalty;

    [Header("Display")]
    [ResizableTextArea] 
    public string description;

    [Header("Roster Cost")]
    public int cost = 4; // 기본 비용. 종족별로 다르게 하려면 RaceData의 armorCostOverrides에 등록
}