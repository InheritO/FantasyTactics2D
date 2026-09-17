using UnityEngine;

[CreateAssetMenu(fileName = "NewFaction", menuName = "Strategy/Faction")]
public class FactionData : ScriptableObject
{
    public string factionName;

    public RaceData race;
    public Color factionColor = Color.white;
    public bool isPlayerControlled = false;
}