using UnityEngine;

/// <summary>
/// 하나의 타일 정보를 담는 클래스 (좌표, 이동 가능 여부, 점유 유닛 등)
/// </summary>
[System.Serializable]
public class TilePosition
{
    // 그리드 상의 좌표 (예: (3, 5))
    public Vector2Int GridCoord { get; private set; }

    // 이 타일에 서 있는 유닛 (없으면 null)
    public UnitBase OccupyingUnit { get; set; }

    // 이동 가능한 타일인지 여부 (벽, 물 등은 false)
    public bool IsWalkable { get; set; } = true;

    // 이 타일을 지나갈 때 드는 이동력 비용 (평지 1, 숲 2 등)
    public int MovementCost { get; set; } = 1;

    public TilePosition(Vector2Int gridCoord)
    {
        GridCoord = gridCoord;
    }

    // 현재 타일이 비어있는지 (유닛이 없고 걸을 수 있는지)
    public bool IsEmpty()
    {
        return IsWalkable && OccupyingUnit == null;
    }

    // 그리드 좌표 -> 월드 좌표 변환 (타일 한 칸 크기 = tileSize)
    public static Vector3 GridToWorld(Vector2Int gridCoord, float tileSize = 1f)
    {
        return new Vector3(gridCoord.x * tileSize, 0f, gridCoord.y * tileSize);
    }

    // 월드 좌표 -> 그리드 좌표 변환
    public static Vector2Int WorldToGrid(Vector3 worldPos, float tileSize = 1f)
    {
        int x = Mathf.RoundToInt(worldPos.x / tileSize);
        int y = Mathf.RoundToInt(worldPos.z / tileSize);
        return new Vector2Int(x, y);
    }

    // 두 타일 간 맨해튼 거리 (사각형 그리드 기준, 이동 범위 계산에 유용)
    public static int GetDistance(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }
}