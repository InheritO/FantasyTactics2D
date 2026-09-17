using System.Collections.Generic;

/// <summary>
/// 대전(Skirmish) 한 판에서 한쪽 진영을 구성하는 정보.
/// 세력(색상, AI 여부 등 정체성)은 유지하되, 종족은 이 판에서 플레이어가 고른 것을 따른다.
/// </summary>
public class SkirmishParticipant
{
    public FactionData Faction { get; }
    public RaceData Race { get; }
    public IReadOnlyList<RosterEntry> Roster { get; }

    public SkirmishParticipant(FactionData faction, RaceData race, IReadOnlyList<RosterEntry> roster)
    {
        Faction = faction;
        Race = race;
        Roster = roster;
    }
}