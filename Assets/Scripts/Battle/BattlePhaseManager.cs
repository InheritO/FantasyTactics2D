using UnityEngine;

public enum BattlePhase
{
    Placement,
    Battle,
    Ended
}

/// <summary>
/// 전투의 현재 페이즈(배치 중 / 전투 중)를 관리한다.
/// 배치 컨트롤러와 이동/선택 컨트롤러는 이 매니저를 참조해서 자신의 활성화 여부를 결정한다.
/// </summary>
public class BattlePhaseManager : MonoBehaviour
{
    [Header("References")]
    public TurnManager turnManager;
    public BattleOutcomeManager outcomeManager;

    public BattlePhase CurrentPhase { get; private set; } = 0;

    void OnEnable()
    {
        if (outcomeManager == null)
        {
            Debug.LogError($"[{name}] outcomeManager가 연결되지 않았습니다.", this);
            return;
        }

        outcomeManager.OnBattleEnded += HandleBattleEnded;
    }

    void OnDisable()
    {
        if (outcomeManager != null)
            outcomeManager.OnBattleEnded -= HandleBattleEnded;
    }



    public void StartBattle()
    {
        if (CurrentPhase == BattlePhase.Battle)
            return;

        if (turnManager == null)
        {
            Debug.LogError($"[{name}] turnManager가 연결되지 않아 전투를 시작할 수 없습니다.", this);
            return;
        }

        CurrentPhase = BattlePhase.Battle;
        Debug.Log("전투 시작. 배치 페이즈 종료.");

        turnManager.InitializeTurnOrder();
    }

    private void HandleBattleEnded(FactionData winner)
    {
        CurrentPhase = BattlePhase.Ended;
        Debug.Log("전투가 종료되어 더 이상 조작할 수 없습니다.");
        // 다음 단계: 승리/패배 화면 표시, 월드맵으로 복귀 트리거 등
    }

    // 테스트 편의를 위해 배치 페이즈로 되돌리는 기능도 추가
    public void ReturnToPlacement()
    {
        CurrentPhase = BattlePhase.Placement;
        Debug.Log("배치 페이즈로 복귀.");
    }

}