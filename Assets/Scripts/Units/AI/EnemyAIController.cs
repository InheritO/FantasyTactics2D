using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;


/// <summary>
/// 플레이어가 아닌 세력의 턴을 자동으로 처리한다.
/// </summary>
public class EnemyAIController : MonoBehaviour
{
    public GridManager gridManager;
    public TurnManager turnManager;

    [Header("AI Pace")]
    public float delayBetweenUnits = 0.3f;

    void OnEnable()
    {
        turnManager.OnTurnStarted += HandleTurnStarted;
    }

    void OnDisable()
    {
        turnManager.OnTurnStarted -= HandleTurnStarted;
    }

    private void HandleTurnStarted(FactionData faction)
    {
        if (faction.isPlayerControlled)
            return;

        Debug.Log($"{faction.name} has Turn.");
        StartCoroutine(RunAITurn(faction));
    }

    private IEnumerator RunAITurn(FactionData faction)
    {
        UnitBase[] allUnits = FindObjectsByType<UnitBase>();

        List<UnitBase> myUnits = allUnits.Where(u => u.Faction == faction).ToList();
        List<UnitBase> enemyUnits = allUnits.Where(u => u.Faction != faction && u.Faction != null).ToList();

        Debug.Log($"[AI] {faction.factionName} 턴. 내 유닛: {myUnits.Count}, 적 유닛: {enemyUnits.Count}");

        foreach (var unit in myUnits)
        {
            if (unit == null || unit.HasActedThisTurn)
            {
                Debug.Log($"[AI] {unit?.name ?? "null"} 스킵됨 (null 이거나 이미 행동함)");
                continue;
            }

            Debug.Log($"[AI] {unit.name} 처리 중. AIBehavior 있음: {unit.AIBehavior != null}");

            unit.AIBehavior?.TakeTurn(unit, gridManager, faction, enemyUnits);
            unit.MarkAsActed();

            yield return new WaitForSeconds(delayBetweenUnits);
        }

        Debug.Log("[AI] 턴 종료, EndTurn 호출");
        turnManager.EndTurn();
    }
}