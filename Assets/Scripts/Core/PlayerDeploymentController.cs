using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 확정된 로스터(ConfirmedEntries)를 플레이어 배치 구역 안에서 하나씩 클릭으로 배치한다.
/// 모든 유닛을 배치하면 자동으로 전투 시작이 가능한 상태가 된다.
/// </summary>
public class PlayerDeploymentController : MonoBehaviour
{
    [Header("References")]
    public GridManager gridManager;
    public BattlePhaseManager phaseManager;
    public BattleOutcomeManager outcomeManager;
    public CombatLogger combatLogger;
    public RosterPhaseManager rosterManager; // 세력/종족/로스터 정보를 전부 여기서 가져옴

    private SkirmishParticipant participant;
    public TestUnit unitPrefab; // 나중에 종족별 프리팹이 생기면 교체될 자리

    public event System.Action OnAllUnitsDeployed;

    private Queue<RosterEntry> pendingEntries = new Queue<RosterEntry>();
    public int RemainingCount => pendingEntries.Count;

    private GameControls controls;

    void Awake()
    {
        controls = new GameControls();
    }

    void OnEnable()
    {
        controls.GamePlay.Enable();
        participant = rosterManager.BuildPlayerParticipant();
        BuildPendingQueue();
    }

    void OnDisable() => controls.GamePlay.Disable();


    private void BuildPendingQueue()
    {
        pendingEntries.Clear();

        if (rosterManager.ConfirmedEntries == null)
        {
            Debug.LogWarning("확정된 로스터가 없습니다.");
            return;
        }

        foreach (var entry in rosterManager.ConfirmedEntries)
            pendingEntries.Enqueue(entry);
    }

    void Update()
    {
        if (phaseManager.CurrentPhase != BattlePhase.Placement)
            return;

        if (pendingEntries.Count == 0)
            return;

        if (controls.GamePlay.Click.WasPressedThisFrame())
            TryDeployAtMouse();
    }

    private void TryDeployAtMouse()
    {
        if (participant == null)
        {
            Debug.LogWarning("참가자 정보가 없어 배치할 수 없습니다.");
            return;
        }

        Vector2 screenPos = controls.GamePlay.Point.ReadValue<Vector2>();
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(screenPos);
        mouseWorldPos.z = 0f;
        Vector2Int coord = gridManager.WorldToGrid(mouseWorldPos);

        TileInstance tile = gridManager.GetTile(coord);

        if (tile == null || tile.Zone != DeploymentZone.PlayerZone)
        {
            Debug.Log("플레이어 배치 구역이 아닙니다.");
            return;
        }

        if (!tile.IsWalkable())
        {
            Debug.Log("이동 불가 타일에는 배치할 수 없습니다.");
            return;
        }

        RosterEntry entry = pendingEntries.Peek();
        UnitBase unit = UnitSpawner.Spawn(unitPrefab, coord, participant, entry, gridManager, outcomeManager, combatLogger);

        if (unit != null)
        {
            pendingEntries.Dequeue();

            if (pendingEntries.Count == 0)
                OnAllUnitsDeployed?.Invoke();
        }
    }
}