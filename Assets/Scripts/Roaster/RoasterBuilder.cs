using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 포인트 한도 안에서 RosterEntry(유닛+장비 세트) 목록을 관리한다.
/// MapGenerator, MovementRangeCalculator와 같은 패턴: MonoBehaviour가 아닌 순수 로직 클래스.
/// </summary>
public class RosterBuilder
{
    public RaceData SelectedRace { get; private set; }
    public int TotalPoints { get; private set; }

    private List<RosterEntry> entries = new List<RosterEntry>();

    public int UsedPoints => entries.Sum(e => e.GetTotalCost(SelectedRace));
    public int RemainingPoints => TotalPoints - UsedPoints;

    public RosterBuilder(RaceData race, int totalPoints)
    {
        SelectedRace = race;
        TotalPoints = totalPoints;
    }

    // 편성 목록에 유닛 한 기 추가 시도 (포인트 초과하면 실패)
    public bool TryAddEntry(RosterEntry entry)
    {
        if (entry == null)
        {
            Debug.LogWarning("TryAddEntry에 null 엔트리가 전달되었습니다.");
            return false;
        }

        int cost = entry.GetTotalCost(SelectedRace);

        if (cost > RemainingPoints)
        {
            Debug.Log($"포인트 부족: 필요 {cost}, 남은 포인트 {RemainingPoints}");
            return false;
        }

        entries.Add(entry);
        return true;
    }

    public void RemoveEntry(RosterEntry entry) => entries.Remove(entry);

    public IReadOnlyList<RosterEntry> GetEntries() => entries;
}