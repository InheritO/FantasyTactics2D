using UnityEngine;
using NaughtyAttributes;

public class GridManager : MonoBehaviour
{
    [Header("Visualizer Reference (선택)")]
    public TileVisualizer tileVisualizer;

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
    }

    [Button]
    public void ClearMapAndVisuals()
    {
        ClearMap();

        if (tileVisualizer != null)
            tileVisualizer.ClearVisuals();
    }

    private void ClearMap()
    {
        tiles = null;
    }

    public TileInstance GetTile(Vector2Int coord)
    {
        if (coord.x < 0 || coord.x >= width || coord.y < 0 || coord.y >= height)
            return null;
        return tiles[coord.x, coord.y];
    }

    public Vector3 GridToWorld(Vector2Int gridCoord) =>
        new Vector3(gridCoord.x * tileSize, gridCoord.y * tileSize, 0f);

    public Vector2Int WorldToGrid(Vector3 worldPos) =>
        new Vector2Int(Mathf.RoundToInt(worldPos.x / tileSize), Mathf.RoundToInt(worldPos.y / tileSize));

    public int GetDistance(Vector2Int a, Vector2Int b) =>
        Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
}