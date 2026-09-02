using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BattleOutcomeManager : MonoBehaviour
{
    public event Action<FactionData> OnFactionDefeated;
    public event Action OnBattleEnded;

    public TurnManager turnManager;

    private HashSet<FactionData> defeatedFactions = new HashSet<FactionData>();

    // 유닛이 스폰될 때마다(혹은 전투 시작 시 한 번) 이 함수로 사망 이벤트를 구독시켜야 함
    public void RegisterUnit(UnitBase unit)
    {
        unit.OnDied += HandleUnitDied;
    }

    private void HandleUnitDied(UnitBase deadUnit)
    {
        FactionData faction = deadUnit.Faction;
        if (faction == null || defeatedFactions.Contains(faction))
            return;

        // 이 세력의 살아있는 유닛이 더 있는지 확인 (죽는 유닛 자신은 아직 Destroy 전이라 제외하고 셈)
        bool hasSurvivors = FindObjectsByType<UnitBase>()
            .Any(u => u != deadUnit && u.Faction == faction);

        if (hasSurvivors)
            return;

        defeatedFactions.Add(faction);
        Debug.Log($"{faction.factionName} 전멸.");
        OnFactionDefeated?.Invoke(faction);

        turnManager.RemoveFaction(faction);

        CheckBattleEnd();
    }

    private void CheckBattleEnd()
    {
        int aliveFactionCount = FindObjectsByType<UnitBase>()
            .Select(u => u.Faction)
            .Where(f => f != null)
            .Distinct()
            .Count();

        if (aliveFactionCount <= 1)
        {
            Debug.Log("전투 종료.");
            OnBattleEnded?.Invoke();
        }
    }
}