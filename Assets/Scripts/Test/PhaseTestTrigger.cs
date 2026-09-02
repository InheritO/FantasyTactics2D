using UnityEngine;

/// <summary>
/// 테스트 편의를 위해 Enter 키로 배치 페이즈 -> 전투 페이즈 전환.
/// 실제 게임에서는 "전투 시작" 버튼 UI 등으로 대체될 예정.
/// </summary>
public class PhaseTestTrigger : MonoBehaviour
{
    public BattlePhaseManager phaseManager;
    public TurnManager turnManager;


    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return))
        {
            phaseManager.StartBattle();
        }

        if (phaseManager.CurrentPhase == BattlePhase.Battle && Input.GetKeyDown(KeyCode.Space))
        {
            turnManager.EndTurn();
        }
    }
}