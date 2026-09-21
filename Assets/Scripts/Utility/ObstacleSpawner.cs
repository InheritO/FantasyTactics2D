using System.Collections.Generic;
using UnityEngine;

public class ObstacleSpawner : MonoBehaviour
{
    public GameObject obstaclePrefab;

    [Header("Noise Settings (지형 생성과 별개)")]
    public float noiseScale = 6f;
    [Range(0f, 1f)] public float threshold = 0.75f; // 이 값 이상이면 장애물 후보

    private readonly List<GameObject> spawnedObstacles = new List<GameObject>();

    public void SpawnObstacles(GridManager gridManager)
    {
        ClearObstacles();

        if (obstaclePrefab == null)
        {
            Debug.LogWarning("ObstacleSpawner: obstaclePrefab이 비어있습니다.");
            return;
        }

        float noiseOffset = Random.Range(0f, 10000f); // 지형 노이즈 패턴이랑 안 겹치게 매번 다르게

        List<Vector2Int> placements = ObstaclePlacementCalculator.CalculatePlacements(
            gridManager, gridManager.width, gridManager.height, noiseScale, noiseOffset, threshold);

        foreach (var coord in placements)
        {
            GameObject obj = Instantiate(obstaclePrefab, gridManager.transform);
            UnitBase unit = obj.GetComponent<UnitBase>();

            if (unit == null)
            {
                Debug.LogWarning("ObstacleSpawner: obstaclePrefab에 UnitBase가 없습니다.");
                Destroy(obj);
                continue;
            }

            unit.PlaceOnGrid(coord, gridManager);
            spawnedObstacles.Add(obj);
        }
    }

    public void ClearObstacles()
    {
        foreach (var obj in spawnedObstacles)
            if (obj != null)
                Destroy(obj);

        spawnedObstacles.Clear();
    }
}