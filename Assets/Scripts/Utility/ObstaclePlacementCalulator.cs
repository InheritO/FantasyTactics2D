using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 노이즈 기반으로 장애물 유닛을 놓을 위치를 계산한다. 배치 구역은 후보에서 제외하고,
/// 놓았을 때 걸을 수 있는 영역이 끊기는 자리는 자동으로 건너뛴다.
/// CombatResolver/MovementRangeCalculator와 같은 패턴: 계산만 하고 실제 생성은 호출한 쪽이 담당한다.
/// </summary>
public static class ObstaclePlacementCalculator
{
    private static readonly Vector2Int[] AdjacentOffsets =
    {
        new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1)
    };

    public static List<Vector2Int> CalculatePlacements(
    GridManager gridManager, int width, int height,
    float noiseScale, float noiseOffset, float threshold)
    {
        List<Vector2Int> candidates = new List<Vector2Int>();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector2Int coord = new Vector2Int(x, y);
                TileInstance tile = gridManager.GetTile(coord);

                if (tile == null || !tile.IsWalkable() || tile.Zone != DeploymentZone.None)
                    continue;

                float noiseValue = Mathf.PerlinNoise(
                    (x + noiseOffset) / noiseScale,
                    (y + noiseOffset) / noiseScale);

                if (noiseValue >= threshold)
                    candidates.Add(coord);
            }
        }

        // 우선 후보를 전부 한 번에 막아보고 연결성을 딱 한 번만 확인한다.
        // threshold가 적당하면 대부분 여기서 끝나서, 후보 수만큼 BFS를 반복할 필요가 없다.
        HashSet<Vector2Int> allBlocked = new HashSet<Vector2Int>(candidates);
        if (StaysFullyConnected(gridManager, width, height, allBlocked))
            return candidates;

        // 한 번에 다 막으면 끊기는 경우에만, 기존처럼 하나씩 시도하며 걸러낸다.
        HashSet<Vector2Int> blocked = new HashSet<Vector2Int>();
        List<Vector2Int> confirmed = new List<Vector2Int>();

        foreach (var coord in candidates)
        {
            blocked.Add(coord);

            if (StaysFullyConnected(gridManager, width, height, blocked))
                confirmed.Add(coord);
            else
                blocked.Remove(coord);
        }

        return confirmed;
    }

    // blocked에 포함된 좌표를 제외한 모든 "걸을 수 있는" 타일이 서로 하나로 이어져 있는지 확인한다.
    private static bool StaysFullyConnected(GridManager gridManager, int width, int height, HashSet<Vector2Int> blocked)
    {
        Vector2Int? start = null;
        int totalWalkable = 0;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector2Int coord = new Vector2Int(x, y);
                TileInstance tile = gridManager.GetTile(coord);

                if (tile == null || !tile.IsWalkable() || blocked.Contains(coord))
                    continue;

                totalWalkable++;
                if (start == null)
                    start = coord;
            }
        }

        if (start == null)
            return true; // 걸을 수 있는 타일이 아예 없는 극단적 상황이면 판단할 게 없으니 통과 처리

        HashSet<Vector2Int> visited = new HashSet<Vector2Int> { start.Value };
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        queue.Enqueue(start.Value);

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();

            foreach (var offset in AdjacentOffsets)
            {
                Vector2Int next = current + offset;
                if (visited.Contains(next) || blocked.Contains(next))
                    continue;

                TileInstance nextTile = gridManager.GetTile(next);
                if (nextTile == null || !nextTile.IsWalkable())
                    continue;

                visited.Add(next);
                queue.Enqueue(next);
            }
        }

        return visited.Count == totalWalkable;
    }
}