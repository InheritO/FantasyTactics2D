using UnityEngine;
using System.Collections;

/// <summary>
/// 대전 플레이의 최상위 흐름(편성 → 전투)을 오브젝트 활성화로 제어한다.
/// 편성이 끝나기 전엔 배틀 관련 오브젝트 자체가 꺼져있어서,
/// BattlePhase 값과 무관하게 물리적으로 상호작용이 차단된다.
/// </summary>
public class SkirmishFlowController : MonoBehaviour
{
    [Header("References")]
    public RosterPhaseManager rosterManager;
    public PlayerDeploymentController playerDeployment;
    public EnemyDeploymentController enemyDeployment;
    public BattleOutcomeManager outcomeManager;
    public TurnManager turnManager;
    public BattlePhaseManager phaseManager;
    public GridManager gridManager;
    public TileVisualizer tileVisualizer;
    public BattleResultPanel resultPanel;

    [Header("UI Panels (같은 Canvas 하위)")]
    public GameObject rosterUIPanel;
    public GameObject battleUIPanel;

    [Header("World Objects")]
    public GameObject battleRoot;   // GridManager, TileVisualizer, 배치/전투 관련 오브젝트 전부

    private bool isValid;

    void Awake()
    {
        isValid = ValidateReferences();

        if (!isValid)
            return;

        rosterUIPanel.SetActive(true);
        battleUIPanel.SetActive(false);
        battleRoot.SetActive(false);
    }

    void OnEnable()
    {
        if (!isValid)
            return;

        rosterManager.OnRosterConfirmed += HandleRosterConfirmed;
        outcomeManager.OnBattleEnded += HandleBattleEnded;
        resultPanel.OnReturnRequested += HandleReturnRequested;
    }

    void OnDisable()
    {
        if (!isValid)
            return;

        rosterManager.OnRosterConfirmed -= HandleRosterConfirmed;
        outcomeManager.OnBattleEnded -= HandleBattleEnded;
        resultPanel.OnReturnRequested -= HandleReturnRequested;
    }

    private void HandleRosterConfirmed()
    {
        rosterUIPanel.SetActive(false);
        battleUIPanel.SetActive(true);
        battleRoot.SetActive(true);

        StartCoroutine(DeployAfterActivation());
    }

    private bool ValidateReferences()
    {
        bool ok = true;

        if (rosterManager == null) { Debug.LogError($"[{name}] rosterManager가 연결되지 않았습니다."); ok = false; }
        if (playerDeployment == null) { Debug.LogError($"[{name}] playerDeployment가 연결되지 않았습니다."); ok = false; }
        if (enemyDeployment == null) { Debug.LogError($"[{name}] enemyDeployment가 연결되지 않았습니다."); ok = false; }
        if (outcomeManager == null) { Debug.LogError($"[{name}] outcomeManager가 연결되지 않았습니다."); ok = false; }
        if (turnManager == null) { Debug.LogError($"[{name}] turnManager가 연결되지 않았습니다."); ok = false; }
        if (phaseManager == null) { Debug.LogError($"[{name}] phaseManager가 연결되지 않았습니다."); ok = false; }
        if (gridManager == null) { Debug.LogError($"[{name}] gridManager가 연결되지 않았습니다."); ok = false; }
        if (tileVisualizer == null) { Debug.LogError($"[{name}] tileVisualizer가 연결되지 않았습니다."); ok = false; }
        if (resultPanel == null) { Debug.LogError($"[{name}] resultPanel이 연결되지 않았습니다."); ok = false; }
        if (rosterUIPanel == null) { Debug.LogError($"[{name}] rosterUIPanel이 연결되지 않았습니다."); ok = false; }
        if (battleUIPanel == null) { Debug.LogError($"[{name}] battleUIPanel이 연결되지 않았습니다."); ok = false; }
        if (battleRoot == null) { Debug.LogError($"[{name}] battleRoot가 연결되지 않았습니다."); ok = false; }

        return ok;
    }

    private IEnumerator DeployAfterActivation()
    {
        yield return null; // 한 프레임 대기

        // AI는 같은 로스터를 그대로 복사해서 사용 (임시. 나중에 별도 AI 자동 편성으로 교체 예정)
        SkirmishParticipant enemyParticipant = rosterManager.BuildEnemyParticipant();
        enemyDeployment.DeployRoster(enemyParticipant);
    }

    private void HandleBattleEnded(FactionData winner)
    {
        bool playerWon = winner != null && winner.isPlayerControlled;
        resultPanel.Show(playerWon);
    }

    private void HandleReturnRequested()
    {
        // 1. 씬에 남아있는 모든 유닛 정리
        UnitBase[] units = FindObjectsByType<UnitBase>();
        foreach (var unit in units)
            Destroy(unit.gameObject);

        // 2. 각 매니저 상태 초기화
        turnManager.ResetState();
        phaseManager.ReturnToPlacement();
        rosterManager.ResetRoster();

        // 3. 맵을 새로 생성해서 다음 판을 깨끗하게 시작
        gridManager.GenerateNewMap();
        tileVisualizer.ClearVisuals();
        tileVisualizer.VisualizeMap();

        // 4. 화면 전환
        resultPanel.Hide();
        battleUIPanel.SetActive(false);
        battleRoot.SetActive(false);
        rosterUIPanel.SetActive(true);
    }
}