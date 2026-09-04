using UnityEngine;

[CreateAssetMenu(fileName = "NewShield", menuName = "Strategy/Equipment/Shield")]
public class ShieldData : ScriptableObject
{
    public string shieldName;
    public Sprite icon;
    public int defenseBonus;
}