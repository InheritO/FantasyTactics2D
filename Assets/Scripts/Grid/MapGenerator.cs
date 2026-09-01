using UnityEngine;

/// <summary>
/// Perlin Noise 기반 무작위 맵 생성기.
/// MonoBehaviour가 아닌 순수 로직 클래스로, GridManager에서 필요할 때 인스턴스화해서 사용한다.
/// </summary>

[System.Serializable]
public struct NoiseTileMapping
{
    public TileTypeData tileType;
    public float maxNoiseValue; // 이 값 미만이면 이 타입 (오름차순으로 배열에 넣어야 함)
}


public class MapGenerator
{
    private float noiseScale;
    private int seed;
    private bool useRandomSeed;
    private NoiseTileMapping[] mappings;

    public MapGenerator(float noiseScale, bool useRandomSeed, int seed, NoiseTileMapping[] mappings)
    {
        this.noiseScale = noiseScale;
        this.useRandomSeed = useRandomSeed;
        this.seed = seed;
        this.mappings = mappings;
    }

    public TileInstance[,] GenerateMap(int width, int height)
    {
        TileInstance[,] tiles = new TileInstance[width, height];

        if (useRandomSeed)
            seed = Random.Range(0, 100000);

        float offsetX = seed * 0.1f;
        float offsetY = seed * 0.1f;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                float noiseValue = Mathf.PerlinNoise(
                    (x + offsetX) / noiseScale,
                    (y + offsetY) / noiseScale
                );

                TileTypeData type = GetTileTypeFromNoise(noiseValue);
                tiles[x, y] = new TileInstance(new Vector2Int(x, y), type);
            }
        }

        return tiles;
    }

    private TileTypeData GetTileTypeFromNoise(float noiseValue)
    {
        foreach (var mapping in mappings)
        {
            if (noiseValue < mapping.maxNoiseValue)
                return mapping.tileType;
        }
        return mappings[mappings.Length - 1].tileType;
    }
}