using UnityEngine;
using System.Collections.Generic;

public class UnitSelectionController : MonoBehaviour
{
    [Header("References")]
    public GridManager gridManager;
    public MovementRangeVisualizer rangeVisualizer;
    public TurnManager turnManager;
    public BattlePhaseManager phaseManager;

    private UnitBase selectedUnit;
    private SpriteRenderer selectedUnitRenderer;
    private Color originalColor;
    private Dictionary<Vector2Int, int> currentReachableTiles;

    void Start()
    {
        rangeVisualizer.Setup(gridManager);
    }


    void Update()
    {
        if (phaseManager.CurrentPhase != BattlePhase.Battle) return;

        if (Input.GetMouseButtonDown(0))
        {
            HandleClick();
        }
    }

    private void HandleClick()
    {
        Vector2Int clickedCoord = GetClickedGridCoord();
        TileInstance clickedTile = gridManager.GetTile(clickedCoord);

        if (clickedTile == null)
            return;

        if (selectedUnit == null)
        {
            TrySelectUnit(clickedTile);
        }
        else
        {
            HandleClickWhileUnitSelected(clickedTile, clickedCoord);
        }
    }

    private Vector2Int GetClickedGridCoord()
    {
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0f;
        return gridManager.WorldToGrid(mouseWorldPos);
    }

    private void TrySelectUnit(TileInstance tile)
    {
        if (tile.OccupyingUnit == null)
            return;

        UnitBase unit = tile.OccupyingUnit;

        // 현재 턴의 세력이 아니거나, 이미 행동한 유닛이면 선택 불가
        if (unit.Faction != turnManager.CurrentFaction)
        {
            Debug.Log("현재 턴의 세력이 아닙니다.");
            return;
        }

        if (unit.HasActedThisTurn)
        {
            Debug.Log("이미 이번 턴에 행동한 유닛입니다.");
            return;
        }

        SelectUnit(unit);
    }

    private void HandleClickWhileUnitSelected(TileInstance clickedTile, Vector2Int clickedCoord)
    {
        if (clickedTile.OccupyingUnit == selectedUnit)
        {
            DeselectUnit();
            return;
        }

        UnitBase targetUnit = clickedTile.OccupyingUnit;

        if (targetUnit != null)
        {
            // 다른 세력 유닛이면 공격 시도, 같은 세력이면 선택 대상 변경
            if (targetUnit.Faction != selectedUnit.Faction)
            {
                TryAttackTarget(targetUnit);
            }
            else
            {
                DeselectUnit();
                TrySelectUnit(clickedTile);
            }
            return;
        }

        // 빈 타일 클릭 -> 이동 시도 (기존과 동일)
        if (currentReachableTiles != null && currentReachableTiles.ContainsKey(clickedCoord))
        {
            bool moved = selectedUnit.TryMoveTo(clickedCoord);
            if (moved)
                selectedUnit.MarkAsActed();
            else
                Debug.Log($"이동 실패: {clickedCoord}");
        }
        else
        {
            Debug.Log($"이동 범위 밖입니다: {clickedCoord}");
        }

        DeselectUnit();
    }

    private void TryAttackTarget(UnitBase target)
    {
        bool attacked = selectedUnit.TryAttack(target);

        if (attacked)
        {
            Debug.Log($"{selectedUnit.name}이(가) {target.name}을(를) 공격했습니다.");
            selectedUnit.MarkAsActed();
        }
        else
        {
            Debug.Log("공격 사거리 밖입니다.");
        }

        DeselectUnit();
    }


    private void SelectUnit(UnitBase unit)
    {
        selectedUnit = unit;
        selectedUnitRenderer = unit.GetComponent<SpriteRenderer>();

        if (selectedUnitRenderer != null)
        {
            originalColor = selectedUnitRenderer.color;
            selectedUnitRenderer.color = Color.yellow;
        }

        currentReachableTiles = MovementRangeCalculator.CalculateReachableTiles(
            gridManager, unit.GridCoord, unit.MoveRange);

        rangeVisualizer.ShowRange(currentReachableTiles);
    }

    private void DeselectUnit()
    {
        if (selectedUnitRenderer != null)
            selectedUnitRenderer.color = originalColor;

        selectedUnit = null;
        selectedUnitRenderer = null;
        currentReachableTiles = null;

        rangeVisualizer.ClearRange();
    }
}