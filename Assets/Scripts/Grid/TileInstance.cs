using UnityEngine;

public enum DeploymentZone
{
    None,       // 배치 불가 구역
    PlayerZone,
    EnemyZone
}

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

    public DeploymentZone Zone { get; set; } = DeploymentZone.None;

    public int GetMovementCost() => TypeData.movementCost;

    public bool IsWalkable() => TypeData.isWalkable && OccupyingUnit == null;

    public bool BlocksLineOfSight() => TypeData.blocksLineOfSight;

    public int GetDefenseBonus() => TypeData.defenseBonus;
}