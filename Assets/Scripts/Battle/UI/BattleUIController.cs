using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 전투 UI의 최상위 버튼들(전투 시작, 턴 종료, 행동 패널 토글)을 관리한다.
/// 현재 페이즈/턴에 따라 버튼의 표시 여부와 활성화 여부를 매 프레임 갱신한다.
/// </summary>
public class BattleUIController : MonoBehaviour
{
    [Header("References")]
    public BattlePhaseManager phaseManager;
    public TurnManager turnManager;
    public PlayerDeploymentController playerDeployment;
    public UnitSelectionController selectionController;
    public Button optionsButton;
    public Button abandonBattleButton;

    [Header("Popups")]
    public UIPopup optionsPopup;
    public ConfirmationPopup abandonConfirmPopup;
    public SkirmishFlowController skirmishFlow;

    [Header("Buttons")]
    public Button startBattleButton;
    public Button endTurnButton;
    [Tooltip("선택된 유닛의 공용 행동(방어태세 등) 패널을 껐다 켰다 하는 토글 버튼. R키(ToggleActions)와 같은 동작을 한다.")]
    public Button actionsButton;

    void OnEnable()
    {
        if (startBattleButton == null || endTurnButton == null || actionsButton == null
            || playerDeployment == null || selectionController == null || optionsButton == null || abandonBattleButton == null)
        {
            Debug.LogError($"[{name}] 필요한 참조(startBattleButton/endTurnButton/actionsButton/playerDeployment/selectionController)가 비어있어 초기화를 건너뜁니다.", this);
            return;
        }

        startBattleButton.interactable = false;
        playerDeployment.OnAllUnitsDeployed += HandleAllUnitsDeployed;

        startBattleButton.onClick.AddListener(HandleStartBattleClicked);
        endTurnButton.onClick.AddListener(HandleEndTurnClicked);
        actionsButton.onClick.AddListener(HandleActionsClicked);
        optionsButton.onClick.AddListener(HandleOptionsClicked);
        abandonBattleButton.onClick.AddListener(HandleAbandonClicked);
    }

    void OnDisable()
    {
        if (playerDeployment != null)
            playerDeployment.OnAllUnitsDeployed -= HandleAllUnitsDeployed;

        if (startBattleButton != null)
            startBattleButton.onClick.RemoveListener(HandleStartBattleClicked);

        if (endTurnButton != null)
            endTurnButton.onClick.RemoveListener(HandleEndTurnClicked);

        if (actionsButton != null)
            actionsButton.onClick.RemoveListener(HandleActionsClicked);

        if (optionsButton != null)
            optionsButton.onClick.RemoveListener(HandleOptionsClicked);

        if (abandonBattleButton != null)
            abandonBattleButton.onClick.RemoveListener(HandleAbandonClicked);
    }

    void Update()
    {
        if (phaseManager == null || turnManager == null || startBattleButton == null
            || endTurnButton == null || actionsButton == null || selectionController == null)
            return;

        bool isPlacement = phaseManager.CurrentPhase == BattlePhase.Placement;
        bool isPlayerTurn = phaseManager.CurrentPhase == BattlePhase.Battle
            && turnManager.CurrentFaction != null
            && turnManager.CurrentFaction.isPlayerControlled;

        startBattleButton.gameObject.SetActive(isPlacement);
        endTurnButton.gameObject.SetActive(isPlayerTurn);

        // 유닛이 선택돼 있고 아직 행동 가능할 때만 노출 (공용 행동 패널이 뜰 수 있는 상황과 동일한 조건)
        UnitBase selected = selectionController.SelectedUnit;
        bool canToggleActions = isPlayerTurn && selected != null && selected.CanStillAct;
        actionsButton.gameObject.SetActive(canToggleActions);

        bool isBattlePhase = phaseManager.CurrentPhase == BattlePhase.Battle;
        optionsButton.gameObject.SetActive(isBattlePhase);
        abandonBattleButton.gameObject.SetActive(isBattlePhase);
    }

    private void HandleAllUnitsDeployed()
    {
        startBattleButton.interactable = true;
    }

    private void HandleStartBattleClicked() => phaseManager.StartBattle();
    private void HandleEndTurnClicked() => turnManager.EndTurn();
    private void HandleActionsClicked() => selectionController.ToggleActionsPanel();

    private void HandleOptionsClicked() => PopupCoordinator.Instance.Open(optionsPopup);
    private void HandleAbandonClicked() => abandonConfirmPopup.Open(skirmishFlow.AbandonBattle);
}
