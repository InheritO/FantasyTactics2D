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

}