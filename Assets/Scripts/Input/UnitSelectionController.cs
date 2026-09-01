using UnityEngine;
using System.Collections.Generic;

public class UnitSelectionController : MonoBehaviour
{
    [Header("References")]
    public GridManager gridManager;
    public MovementRangeVisualizer rangeVisualizer;
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

        SelectUnit(tile.OccupyingUnit);
    }

    private void HandleClickWhileUnitSelected(TileInstance clickedTile, Vector2Int clickedCoord)
    {
        if (clickedTile.OccupyingUnit == selectedUnit)
        {
            DeselectUnit();
            return;
        }

        if (clickedTile.OccupyingUnit != null)
        {
            DeselectUnit();
            SelectUnit(clickedTile.OccupyingUnit);
            return;
        }

        // 이동 가능 범위 안에 있는 타일인지 확인
        if (currentReachableTiles != null && currentReachableTiles.ContainsKey(clickedCoord))
        {
            bool moved = selectedUnit.TryMoveTo(clickedCoord);
            if (!moved)
                Debug.Log($"이동 실패: {clickedCoord}");
        }
        else
        {
            Debug.Log($"이동 범위 밖입니다: {clickedCoord}");
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