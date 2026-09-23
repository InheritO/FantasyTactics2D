using UnityEngine;
using NaughtyAttributes;

public enum ZoneAxis
{
    TopBottom,
    LeftRight
    // 나중에 DiagonalCorners 등 추가 가능
}

public class GridManager : MonoBehaviour
{
    [Header("Visualizer Reference (선택)")]
    public TileVisualizer tileVisualizer;
    public TileGridOverlay tileGridOverlay;

    [Header("Obstacle Spawning (선택)")]
    public ObstacleSpawner obstacleSpawner;

    [Header("Grid Settings")]
    public int width = 10;
    public int height = 10;
    public float tileSize = 1f;

    [Header("Map Generation Settings")]
    public float noiseScale = 5f;
    public bool useRandomSeed = true;
    public int seed = 0;

    [Header("Tile Types (노이즈 값 오름차순으로 배치)")]
    public NoiseTileMapping[] tileMappings;

    [Header("Deployment Zones")]
    public ZoneAxis zoneAxis = ZoneAxis.TopBottom;
    [Tooltip("플레이어 배치 구역의 두께 (TopBottom: 아래쪽 행 수 / LeftRight: 왼쪽 열 수)")]
    public int playerZoneDepth = 3;
    [Tooltip("적 배치 구역의 두께 (TopBottom: 위쪽 행 수 / LeftRight: 오른쪽 열 수)")]
    public int enemyZoneDepth = 3;

    private MapGenerator mapGenerator;
    private TileInstance[,] tiles;

    void Awake()
    {
        mapGenerator = new MapGenerator(noiseScale, useRandomSeed, seed, tileMappings);
        GenerateNewMap();
    }

    [Button]
    public void GenerateNewMap()
    {
        tiles = mapGenerator.GenerateMap(width, height);
        ApplyDeploymentZones();

        tileGridOverlay?.Build();
        obstacleSpawner?.SpawnObstacles(this);
    }

    [Button]
    public void ClearMapAndVisuals()
    {
        ClearMap();

        if (tileVisualizer != null)
            tileVisualizer.ClearVisuals();

        if (tileGridOverlay != null)
            tileGridOverlay.Clear();

        obstacleSpawner?.ClearObstacles();
    }

    private void ClearMap()
    {
        tiles = null;
    }

    public TileInstance GetTile(Vector2Int coord)
    {
        // 맵이 아직 생성되지 않았거나(ClearMapAndVisuals 직후 등) GenerateNewMap이 호출되기 전이면
        // tiles가 null일 수 있음. 범위 체크만으로는 이 경우를 걸러내지 못해 NullReferenceException이 났었음.
        if (tiles == null)
            return null;

        if (coord.x < 0 || coord.x >= width || coord.y < 0 || coord.y >= height)
            return null;
        return tiles[coord.x, coord.y];
    }

    private void ApplyDeploymentZones()
    {
        RectInt playerRect;
        RectInt enemyRect;

        switch (zoneAxis)
        {
            case ZoneAxis.LeftRight:
                {
                    int playerDepth = Mathf.Clamp(playerZoneDepth, 0, width);
                    int enemyDepth = Mathf.Clamp(enemyZoneDepth, 0, width);
                    playerRect = new RectInt(0, 0, playerDepth, height);                  // 왼쪽 N열
                    enemyRect = new RectInt(width - enemyDepth, 0, enemyDepth, height);   // 오른쪽 N열
                    break;
                }
            case ZoneAxis.TopBottom:
            default:
                {
                    int playerDepth = Mathf.Clamp(playerZoneDepth, 0, height);
                    int enemyDepth = Mathf.Clamp(enemyZoneDepth, 0, height);
                    playerRect = new RectInt(0, 0, width, playerDepth);                   // 아래쪽 N행
                    enemyRect = new RectInt(0, height - enemyDepth, width, enemyDepth);  // 위쪽 N행
                    break;
                }
        }

        ApplyZoneRect(playerRect, DeploymentZone.PlayerZone);
        ApplyZoneRect(enemyRect, DeploymentZone.EnemyZone);
    }

    private void ApplyZoneRect(RectInt rect, DeploymentZone zone)
    {
        for (int x = rect.xMin; x < rect.xMax; x++)
        {
            for (int y = rect.yMin; y < rect.yMax; y++)
            {
                TileInstance tile = GetTile(new Vector2Int(x, y));
                if (tile != null)
                    tile.Zone = zone;
            }
        }
    }

    public Vector3 GridToWorld(Vector2Int gridCoord) =>
        new Vector3(gridCoord.x * tileSize, gridCoord.y * tileSize, 0f);

    public Vector2Int WorldToGrid(Vector3 worldPos) =>
        new Vector2Int(Mathf.RoundToInt(worldPos.x / tileSize), Mathf.RoundToInt(worldPos.y / tileSize));

    public int GetDistance(Vector2Int a, Vector2Int b) =>
        Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);

    void OnValidate()
    {
        width = Mathf.Max(1, width);
        height = Mathf.Max(1, height);

        playerZoneDepth = Mathf.Max(0, playerZoneDepth);
        enemyZoneDepth = Mathf.Max(0, enemyZoneDepth);

        int mapExtent = zoneAxis == ZoneAxis.LeftRight ? width : height;

        if (playerZoneDepth + enemyZoneDepth > mapExtent)
            Debug.LogWarning($"[{name}] 배치 구역 두께의 합({playerZoneDepth + enemyZoneDepth})이 맵 크기({mapExtent})를 넘어 두 구역이 겹칠 수 있습니다.");
    }
}
