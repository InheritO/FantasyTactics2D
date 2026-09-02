using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;


public class TurnManager : MonoBehaviour
{
    public FactionData CurrentFaction { get; private set; }

       public GridManager gridManager;
    public TurnManager turnManager;

    private List<FactionData> turnOrder = new List<FactionData>();
    private int currentIndex = 0;

    public event Action<FactionData> OnTurnStarted;

    

    public void InitializeTurnOrder()
    {
        UnitBase[] allUnits = FindObjectsByType<UnitBase>();

        turnOrder = allUnits
            .Select(unit => unit.Faction)
            .Where(faction => faction != null)
            .Distinct()
            .ToList();

        currentIndex = 0;

        if (turnOrder.Count == 0)
        {
            Debug.Log("배치된 유닛이 없어 턴을 시작할 수 없습니다.");
            return;
        }

        StartTurnFor(turnOrder[currentIndex]);
    }

    public void EndTurn()
    {
        if (turnOrder.Count == 0) return;

        currentIndex = (currentIndex + 1) % turnOrder.Count;
        StartTurnFor(turnOrder[currentIndex]);
    }

    // 전투 도중 새로운 세력이 난입할 때 호출
    // insertNext: true면 바로 다음w 차례로 끼워넣음, false면 순서 맨 뒤에 추가
    public void AddFaction(FactionData faction, bool insertNext = true)
    {
        if (faction == null || turnOrder.Contains(faction))
            return;

        if (turnOrder.Count == 0)
        {
            // 전투 시작 전이거나 모든 세력이 사라진 상태였다면 이 세력부터 시작
            turnOrder.Add(faction);
            currentIndex = 0;
            StartTurnFor(faction);
            return;
        }

        // currentIndex보다 뒤에 삽입되므로 currentIndex 자체는 안전함
        int insertIndex = insertNext ? currentIndex + 1 : turnOrder.Count;
        turnOrder.Insert(insertIndex, faction);

        Debug.Log($"{faction.factionName} 참전. 턴 순서에 추가됨 (다음 차례: {insertNext}).");
    }

    // 세력이 전멸하는 등 전투에서 제외될 때 호출
    public void RemoveFaction(FactionData faction)
    {
        int removedIndex = turnOrder.IndexOf(faction);
        if (removedIndex == -1)
            return;

        turnOrder.RemoveAt(removedIndex);

        // 제거된 세력이 현재 턴이었거나, currentIndex보다 앞이면 인덱스 보정 필요
        if (removedIndex < currentIndex)
        {
            currentIndex--;
        }
        else if (removedIndex == currentIndex)
        {
            if (turnOrder.Count == 0)
            {
                CurrentFaction = null;
                Debug.Log("모든 세력이 제거되어 전투가 종료됩니다.");
                return;
            }

            currentIndex %= turnOrder.Count; // 범위를 벗어나면 다시 처음으로
            StartTurnFor(turnOrder[currentIndex]);
        }
    }

    private void StartTurnFor(FactionData faction)
    {
        CurrentFaction = faction;
        Debug.Log($"{faction.factionName}의 턴 시작.");

        UnitBase[] allUnits = FindObjectsByType<UnitBase>();
        foreach (var unit in allUnits)
        {
            if (unit.Faction == faction)
                unit.ResetTurnState();
        }

        OnTurnStarted?.Invoke(faction); // 추가
    }
}