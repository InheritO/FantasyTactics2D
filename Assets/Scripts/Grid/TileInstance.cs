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

    // TypeData가 null인 경우(타일 타입 설정 누락)는 예외를 던지는 대신 안전한 기본값으로 처리한다.
    // IsWalkable은 특히 "이동 불가"로 안전하게 막아서, 설정이 잘못된 타일로 유닛이 들어가지 않게 한다.
    public int GetMovementCost() => TypeData != null ? TypeData.movementCost : 1;

    public bool IsWalkable()
    {
        if (TypeData == null)
        {
            Debug.LogError($"[TileInstance] {GridCoord} 타일에 TypeData가 없어 이동 불가로 처리합니다. GridManager의 Tile Types 설정을 확인하세요.");
            return false;
        }

        return TypeData.isWalkable && OccupyingUnit == null;
    }

    public bool BlocksLineOfSight() => TypeData != null && TypeData.blocksLineOfSight;

    public int GetDefenseBonus() => TypeData != null ? TypeData.defenseBonus : 0;

    public bool ProvidesCover() => TypeData != null && TypeData.providesCover;
}