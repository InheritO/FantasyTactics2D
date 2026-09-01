using UnityEngine;

[System.Serializable]
public class TileInstance
{
    public Vector2Int GridCoord { get; private set; }
    public TileTypeData TypeData { get; private set; }

    public UnitBase OccupyingUnit { get; set; }

    public TileInstance(Vector2Int gridCoord, TileTypeData typeData)
    {
        GridCoord = gridCoord;
        TypeData = typeData;
    }

    public int GetMovementCost() => TypeData.movementCost;

    public bool IsWalkable() => TypeData.isWalkable && OccupyingUnit == null;

    public bool BlocksLineOfSight() => TypeData.blocksLineOfSight;

    public int GetDefenseBonus() => TypeData.defenseBonus;
}