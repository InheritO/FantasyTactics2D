using UnityEngine;

[CreateAssetMenu(fileName = "NewTileType", menuName = "Strategy/TileType")]
public class TileTypeData : ScriptableObject
{
    public string tileName;
    public Sprite icon;
    public int movementCost = 1;
    public bool isWalkable = true;
    public bool blocksLineOfSight = false;
    public int defenseBonus = 0;

    [Tooltip("엄폐 등 원거리 공격 방어 증가")]
    public bool providesCover = false; 

    [Header("Prototype Visualization (icon 없을 때 사용)")]
    public Color previewColor = Color.white;
}