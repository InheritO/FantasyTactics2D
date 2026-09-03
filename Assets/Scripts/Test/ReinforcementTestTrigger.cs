using UnityEngine;

/// <summary>
/// 전투 중간에 새 세력이 난입하는 상황을 테스트하기 위한 트리거.
/// 지정된 좌표에 유닛을 스폰하고, 그 세력을 턴 순서에 추가한다.
/// </summary>
public class ReinforcementTestTrigger : MonoBehaviour
{
    public GridManager gridManager;
    public BattlePhaseManager phaseManager;
    public TurnManager turnManager;
    public TestUnit testUnitPrefab;
    public BattleOutcomeManager outcomeManager;

    public FactionData reinforcementFaction;
    public Vector2Int spawnCoord;
    public bool insertAsNextTurn = true;

    void Update()
    {
        if (phaseManager.CurrentPhase != BattlePhase.Battle)
            return;

        if (Input.GetKeyDown(KeyCode.R))
        {
            UnitSpawner.Spawn(testUnitPrefab, spawnCoord, reinforcementFaction, gridManager, outcomeManager);
            turnManager.AddFaction(reinforcementFaction, insertAsNextTurn);
        }
    }
}