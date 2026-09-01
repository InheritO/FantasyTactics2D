using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 유닛의 이동력을 기준으로 도달 가능한 타일들을 계산한다.
/// 타일마다 이동 비용이 다르므로 단순 BFS가 아닌 비용 기반 탐색(다익스트라 방식)을 사용한다.
/// </summary>
public class MovementRangeCalculator
{
    private static readonly Vector2Int[] Directions = new Vector2Int[]
    {
        Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
    };

    // 시작 좌표에서 maxMoveRange 이내에 도달 가능한 타일과, 그 타일까지의 최소 이동 비용을 반환
    public static Dictionary<Vector2Int, int> CalculateReachableTiles(
        GridManager gridManager, Vector2Int startCoord, int maxMoveRange)
    {
        Dictionary<Vector2Int, int> costSoFar = new Dictionary<Vector2Int, int>();
        costSoFar[startCoord] = 0;

        // (남은 이동력이 큰 순서가 아니라 누적 비용이 작은 순서로 탐색해야 정확하지만,
        // 이동 비용 값이 작고 맵 규모가 작은 단계라 우선순위 큐 없이 단순 큐로도 충분함)
        Queue<Vector2Int> frontier = new Queue<Vector2Int>();
        frontier.Enqueue(startCoord);

        while (frontier.Count > 0)
        {
            Vector2Int current = frontier.Dequeue();
            int currentCost = costSoFar[current];

            foreach (Vector2Int dir in Directions)
            {
                Vector2Int next = current + dir;
                TileInstance nextTile = gridManager.GetTile(next);

                if (nextTile == null || !nextTile.IsWalkable())
                    continue;

                int newCost = currentCost + nextTile.GetMovementCost();

                if (newCost > maxMoveRange)
                    continue;

                // 아직 방문 안 했거나, 더 저렴한 경로를 찾은 경우에만 갱신
                if (!costSoFar.ContainsKey(next) || newCost < costSoFar[next])
                {
                    costSoFar[next] = newCost;
                    frontier.Enqueue(next);
                }
            }
        }

        costSoFar.Remove(startCoord); // 시작 지점 자신은 "이동 가능 범위"에서 제외
        return costSoFar;
    }
}