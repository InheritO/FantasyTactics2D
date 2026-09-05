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

    public UnitBase SelectedUnit => selectedUnit;

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

        if (!unit.CanStillAct)
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
            if (!moved)
                Debug.Log($"이동 실패: {clickedCoord}");
        }
        else
        {
            Debug.Log($"이동 범위 밖입니다: {clickedCoord}");
        }

        RefreshSelectionDisplay();
    }

    private void TryAttackTarget(UnitBase target)
    {
        bool attacked = selectedUnit.TryAttack(target);

        if (!attacked)
            Debug.Log("공격할 수 없습니다 (사거리 밖이거나 이미 행동함).");

        DeselectUnit(); // 공격은 항상 이동까지 봉인되므로 무조건 선택 해제
    }

    private void RefreshSelection()
    {
        currentReachableTiles = MovementRangeCalculator.CalculateReachableTiles(
            gridManager, selectedUnit.GridCoord, selectedUnit.MoveRange);

        rangeVisualizer.ShowRange(currentReachableTiles);
    }

    private void RefreshSelectionDisplay()
    {
        if (!selectedUnit.CanStillAct)
        {
            DeselectUnit();
            return;
        }

        if (selectedUnit.HasMoved)
        {
            // 이동은 끝났고 공격만 남았으면, 더 이상 "이동 가능 범위"를 보여주지 않음
            rangeVisualizer.ClearRange();
            currentReachableTiles = null;
        }
        else
        {
            // 아직 이동 전이면 (이 분기는 사실 지금 흐름상 거의 안 옴)
            RefreshSelection();
        }
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

        RefreshSelectionDisplay();
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


    //전투 시스템 고도화 + UI 준비될 때까지 사용할 로그 뭉탱이
    private void LogUnitStatus(UnitBase unit)
    {
        string mainHand = unit.MainHandWeapon != null ? unit.MainHandWeapon.weaponName : "없음";
        string offHand = unit.OffHandWeapon != null ? unit.OffHandWeapon.weaponName : "없음";
        string shield = unit.EquippedShield != null ? unit.EquippedShield.shieldName : "없음";
        string armor = unit.EquippedArmor != null ? unit.EquippedArmor.armorName : "없음";

        Debug.Log($"[{unit.name}] 주무기: {mainHand} | 보조무기: {offHand} | 방패: {shield} | 방어구: {armor}\n" +
                  $"이동력: {unit.MoveRange} | 사거리: {unit.AttackRange} | 방어력: {unit.Defense}(맷집{unit.ConstitutionDefense}+장비{unit.ArmorDefense})");
    }
}