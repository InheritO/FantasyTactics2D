using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 전투 UI의 최상위 버튼들(전투 시작, 턴 종료)을 관리한다.
/// 현재 페이즈/턴에 따라 버튼의 표시 여부와 활성화 여부를 매 프레임 갱신한다.
/// </summary>
public class BattleUIController : MonoBehaviour
{
    [Header("References")]
    public BattlePhaseManager phaseManager;
    public TurnManager turnManager;
    public PlayerDeploymentController playerDeployment;

    [Header("Buttons")]
    public Button startBattleButton;
    public Button endTurnButton;

    void OnEnable()
    {
        if (startBattleButton == null || endTurnButton == null || playerDeployment == null)
        {
            Debug.LogError($"[{name}] 필요한 참조(startBattleButton/endTurnButton/playerDeployment)가 비어있어 초기화를 건너뜁니다.", this);
            return;
        }

        startBattleButton.interactable = false;
        playerDeployment.OnAllUnitsDeployed += HandleAllUnitsDeployed;

        startBattleButton.onClick.AddListener(HandleStartBattleClicked);
        endTurnButton.onClick.AddListener(HandleEndTurnClicked);
    }

    void OnDisable()
    {
        if (playerDeployment != null)
            playerDeployment.OnAllUnitsDeployed -= HandleAllUnitsDeployed;

        if (startBattleButton != null)
            startBattleButton.onClick.RemoveListener(HandleStartBattleClicked);

        if (endTurnButton != null)
            endTurnButton.onClick.RemoveListener(HandleEndTurnClicked);
    }

    void Update()
    {
        if (phaseManager == null || turnManager == null || startBattleButton == null || endTurnButton == null)
            return;

        bool isPlacement = phaseManager.CurrentPhase == BattlePhase.Placement;
        bool isPlayerTurn = phaseManager.CurrentPhase == BattlePhase.Battle
            && turnManager.CurrentFaction != null
            && turnManager.CurrentFaction.isPlayerControlled;

        startBattleButton.gameObject.SetActive(isPlacement);
        endTurnButton.gameObject.SetActive(isPlayerTurn);
    }

    private void HandleAllUnitsDeployed()
    {
        startBattleButton.interactable = true;
    }

    private void HandleStartBattleClicked() => phaseManager.StartBattle();
    private void HandleEndTurnClicked() => turnManager.EndTurn();
}