using UnityEngine;
using NaughtyAttributes;

[CreateAssetMenu(fileName = "NewArmor", menuName = "Strategy/Equipment/Armor")]
public class ArmorData : ScriptableObject
{
    public string armorName;
    public Sprite icon;

    [Header("Visuals")]
    public CharacterAnimationSet visualSet;
    [Tooltip("투구가 있는 갑옷 등, 착용 시 머리 모양이 바뀌어야 하면 설정. 비워두면 종족 기본 머리를 그대로 씀")]
    public CharacterAnimationSet headVisualSet;

    [Header("Stat Modifiers")]
    public int defenseBonus;
    public int moveRangePenalty;

    [Header("Display")]
    [ResizableTextArea] 
    public string description;

    [Header("Roster Cost")]
    public int cost = 4; // 기본 비용. 종족별로 다르게 하려면 RaceData의 armorCostOverrides에 등록
}