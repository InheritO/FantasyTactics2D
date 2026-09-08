using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 부대 편성 페이즈를 총괄한다. 종족 선택과 포인트 관리(RosterBuilder에 위임)를 담당하고,
/// 확정된 편성 목록을 Placement 페이즈로 넘겨준다.
/// 실제 화면/버튼 처리는 RosterUIController가 맡고, 이 클래스는 로직만 다룬다.
/// </summary>
public class RosterPhaseManager : MonoBehaviour
{
    [Header("Settings")]
    public RaceData[] availableRaces;
    public int totalPoints = 100;


    public RaceData SelectedRace { get; private set; }
    public RosterBuilder Builder { get; private set; }

    // 확정된 편성 목록. Placement 단계에서 이걸 읽어 실제 유닛을 스폰하게 될 예정
    public IReadOnlyList<RosterEntry> ConfirmedEntries { get; private set; }

    public event Action OnRosterChanged; // 포인트/목록이 바뀔 때마다 (UI 갱신용)
    public event Action OnRosterConfirmed;

    // 종족을 선택(또는 재선택)하면 편성 진행 상황이 초기화됨
    public void SelectRace(RaceData race)
    {
        if (race == null)
        {
            Debug.LogWarning("SelectRace에 null 종족이 전달되었습니다.");
            return;
        }


        SelectedRace = race;
        Builder = new RosterBuilder(race, totalPoints);
        OnRosterChanged?.Invoke();
    }

    public bool TryAddEntry(RosterEntry entry)
    {
        if (Builder == null)
        {
            Debug.LogWarning("종족을 먼저 선택해야 합니다.");
            return false;
        }

        bool success = Builder.TryAddEntry(entry);

        if (success)
            OnRosterChanged?.Invoke();

        return success;
    }

    public void RemoveEntry(RosterEntry entry)
    {
        if (Builder == null)
            return;

        Builder.RemoveEntry(entry);
        OnRosterChanged?.Invoke();
    }

    public void ConfirmRoster()
    {
        if (Builder == null || Builder.GetEntries().Count == 0)
        {
            Debug.LogWarning("편성된 유닛이 없습니다.");
            return;
        }

        ConfirmedEntries = new List<RosterEntry>(Builder.GetEntries());
        OnRosterConfirmed?.Invoke();
    }
}