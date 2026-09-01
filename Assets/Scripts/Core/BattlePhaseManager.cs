using UnityEngine;

public enum BattlePhase
{
    Placement,  // 유닛 배치 중
    Battle      // 턴 기반 전투 진행 중
}

public class BattlePhaseManager : MonoBehaviour
{
    public BattlePhase CurrentPhase { get; private set; } = BattlePhase.Placement;

    public void StartBattle()
    {
        CurrentPhase = BattlePhase.Battle;
        Debug.Log("전투 시작! 배치 페이즈 종료.");
        // 나중에 턴 시스템 시작 트리거를 여기에 연결
    }
}