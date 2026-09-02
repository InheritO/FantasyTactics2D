using UnityEngine;
using NaughtyAttributes;

/// <summary>
/// 테스트 목적으로 클릭한 위치에 자유롭게 유닛을 배치하는 도구.
/// Tab 키로 세력을 순환 선택하고, 마우스 클릭으로 스폰한다.
/// 실제 게임의 전투 시작 배치 로직이 만들어지면 이 스크립트는 제거해도 된다.
/// </summary>
public class TestUnitPlacer : MonoBehaviour
{
    [Header("References")]
    public GridManager gridManager;
    public BattlePhaseManager phaseManager;
    public BattleOutcomeManager outcomeManager;
    [HorizontalLine]
    public TestUnit testUnitPrefab;
    public FactionData[] factions; // 인스펙터에서 2개 이상 자유롭게 등록 가능

    private int currentFactionIndex = 0;

    void Update()
    {
        if (phaseManager.CurrentPhase != BattlePhase.Placement)
            return;

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            CycleFaction();
        }

        if (Input.GetMouseButtonDown(1)) // 우클릭으로 배치 (좌클릭은 선택/이동과 겹치므로)
        {
            PlaceUnitAtMouse();
        }
    }

    private void CycleFaction()
    {
        if (factions.Length == 0) return;

        currentFactionIndex = (currentFactionIndex + 1) % factions.Length;
        Debug.Log($"현재 배치 세력: {factions[currentFactionIndex].factionName}");
    }

    private void PlaceUnitAtMouse()
    {
        if (factions.Length == 0)
        {
            Debug.Log("등록된 세력이 없습니다.");
            return;
        }

        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0f;
        Vector2Int coord = gridManager.WorldToGrid(mouseWorldPos);

        FactionData selectedFaction = factions[currentFactionIndex];
        UnitSpawner.Spawn(testUnitPrefab, coord, selectedFaction, gridManager, outcomeManager);
    }
}