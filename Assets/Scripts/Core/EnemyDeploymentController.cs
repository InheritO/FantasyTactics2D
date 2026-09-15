using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// AI 세력의 확정된 로스터를 적 배치 구역 안에서 무작위 위치에 자동 배치한다.
/// </summary>
public class EnemyDeploymentController : MonoBehaviour
{
    [Header("References")]
    public GridManager gridManager;
    public BattleOutcomeManager outcomeManager;
    public CombatLogger combatLogger;
    public TestUnit unitPrefab;

  


    // RosterPhaseManager가 UI에서 선택된 값을 관리하고, DeployRoster 호출 시 넘겨받는다.
    public void DeployRoster(SkirmishParticipant participant, AICombatDisposition disposition)
    {
        if (participant == null)
            return;

        if (gridManager == null)
        {
            Debug.LogError($"[{name}] gridManager가 연결되지 않아 적을 배치할 수 없습니다.", this);
            return;
        }

        List<Vector2Int> candidateCoords = GetEnemyZoneWalkableCoords();
        Shuffle(candidateCoords);

        int index = 0;

        foreach (var entry in participant.Roster)
        {
            if (index >= candidateCoords.Count)
            {
                Debug.LogWarning("적 배치 구역에 남은 빈 자리가 없습니다.");
                break;
            }

            Vector2Int coord = candidateCoords[index];
            index++;

            UnitSpawner.Spawn(unitPrefab, coord, participant, entry, gridManager, outcomeManager, combatLogger, disposition);
        }
    }

    private List<Vector2Int> GetEnemyZoneWalkableCoords()
    {
        List<Vector2Int> result = new List<Vector2Int>();

        for (int x = 0; x < gridManager.width; x++)
        {
            for (int y = 0; y < gridManager.height; y++)
            {
                Vector2Int coord = new Vector2Int(x, y);
                TileInstance tile = gridManager.GetTile(coord);

                if (tile != null && tile.Zone == DeploymentZone.EnemyZone && tile.IsWalkable())
                    result.Add(coord);
            }
        }

        return result;
    }

    private void Shuffle(List<Vector2Int> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}