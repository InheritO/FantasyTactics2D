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
    public FactionData enemyFaction;

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
    }

    void OnDisable()
    {
        if (!isValid)
            return;

        rosterManager.OnRosterConfirmed -= HandleRosterConfirmed;
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

        if (rosterManager == null)
        {
            Debug.LogError($"[{name}] rosterManager가 연결되지 않았습니다.");
            ok = false;
        }

        if (rosterUIPanel == null)
        {
            Debug.LogError($"[{name}] rosterUIPanel이 연결되지 않았습니다.");
            ok = false;
        }

        if (battleUIPanel == null)
        {
            Debug.LogError($"[{name}] battleUIPanel이 연결되지 않았습니다.");
            ok = false;
        }

        if (battleRoot == null)
        {
            Debug.LogError($"[{name}] battleRoot가 연결되지 않았습니다.");
            ok = false;
        }

        return ok;
    }

    private IEnumerator DeployAfterActivation()
    {
        yield return null; // 한 프레임 대기

        // AI는 같은 로스터를 그대로 복사해서 사용 (임시. 나중에 별도 AI 자동 편성으로 교체 예정)
        enemyDeployment.DeployRoster(enemyFaction, rosterManager.ConfirmedEntries);
    }
}