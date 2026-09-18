using UnityEngine;
using System.Collections.Generic;

public class UnitSelectionController : MonoBehaviour
{
    [Header("References")]
    public GridManager gridManager;
    public MovementRangeVisualizer rangeVisualizer;
    public TurnManager turnManager;
    public BattlePhaseManager phaseManager;
    public BattleAttackPanel attackPanel;

    private UnitBase selectedUnit;
    private SpriteRenderer selectedUnitRenderer;
    private Color originalColor;
    private Dictionary<Vector2Int, int> currentReachableTiles;

    public UnitBase SelectedUnit => selectedUnit;


    private GameControls controls;

    void Awake()
    {
        controls = new GameControls();
    }

    void OnEnable() => controls.GamePlay.Enable();
    void OnDisable() => controls.GamePlay.Disable();

    void Start()
    {
        if (rangeVisualizer == null || gridManager == null)
        {
            Debug.LogError($"[{name}] gridManager 또는 rangeVisualizer가 연결되지 않았습니다.", this);
            return;
        }

        rangeVisualizer.Setup(gridManager);
    }


    void Update()
    {
        if (phaseManager == null || gridManager == null || turnManager == null)
            return;

        if (phaseManager.CurrentPhase != BattlePhase.Battle)
            return;

        if (controls.GamePlay.Click.WasPressedThisFrame())
            HandleClick();

        // 이동 범위 타일을 가릴 때 잠깐 치워두고 싶을 수 있어서, 패널을 껐다 켰다 하는 토글 단축키.
        if (controls.GamePlay.ToggleActions.WasPressedThisFrame())
            ToggleActionsPanel();
    }

    private void HandleClick()
    {
        Vector2Int? clickedCoord = GetClickedGridCoord();
        if (clickedCoord == null)
            return;

        TileInstance clickedTile = gridManager.GetTile(clickedCoord.Value);

        if (clickedTile == null)
            return;

        if (selectedUnit == null)
        {
            TrySelectUnit(clickedTile);
        }
        else
        {
            HandleClickWhileUnitSelected(clickedTile, clickedCoord.Value);
        }
    }

    private Vector2Int? GetClickedGridCoord()
    {
        // MainCamera 태그가 붙은 카메라가 씬에 없거나 일시적으로 비활성화되어 있으면 Camera.main이 null이 되어
        // 클릭할 때마다 NullReferenceException이 났었음.
        if (Camera.main == null)
        {
            Debug.LogError("[UnitSelectionController] Camera.main을 찾을 수 없습니다. MainCamera 태그가 붙은 카메라가 씬에 있는지 확인하세요.");
            return null;
        }

        Vector2 screenPos = controls.GamePlay.Point.ReadValue<Vector2>();
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(screenPos);
        mouseWorldPos.z = 0f;
        return gridManager.WorldToGrid(mouseWorldPos);
    }

    private void TrySelectUnit(TileInstance tile)
    {
        if (tile.OccupyingUnit == null)
            return;

        UnitBase unit = tile.OccupyingUnit;

        if (turnManager.CurrentFaction == null)
        {
            Debug.LogWarning("아직 턴이 시작되지 않아 유닛을 선택할 수 없습니다.");
            return;
        }

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
            if (targetUnit.Faction != selectedUnit.Faction)
            {
                ShowAttackOptions(targetUnit);
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

    private void ShowAttackOptions(UnitBase target)
    {
        if (!selectedUnit.IsInAttackRange(target))
        {
            Debug.Log("사거리 밖입니다.");
            return;
        }

        if (attackPanel == null)
        {
            Debug.LogError($"[{name}] attackPanel이 연결되지 않아 공격 UI를 표시할 수 없습니다.", this);
            return;
        }

        attackPanel.Show(selectedUnit, target, HandleAttackExecuted);
    }

    // 패널이 열려있으면 닫고, 닫혀있으면(대상 없이, 방어태세 등 공용 행동만) 연다.
    // R키(ToggleActions)와 BattleUIController의 토글 버튼 양쪽에서 호출되므로 public.
    public void ToggleActionsPanel()
    {
        if (selectedUnit == null || attackPanel == null)
            return;

        if (attackPanel.IsShown)
        {
            attackPanel.Hide();
            return;
        }

        if (!selectedUnit.CanStillAct)
            return;

        attackPanel.Show(selectedUnit, null, HandleAttackExecuted);
    }

    private void HandleAttackExecuted()
    {
        DeselectUnit();
    }

    private void RefreshSelection()
    {
        currentReachableTiles = MovementRangeCalculator.CalculateReachableTiles(gridManager, selectedUnit); // 변경
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
            // 아직 이동 전 (유닛을 막 선택한 직후 매번 여기로 들어옴)
            RefreshSelection();
        }

        // 대상 없이도 쓸 수 있는 공용 행동(방어태세 등)을 선택과 동시에 보여준다.
        // 이미 열려있으면(예: 적을 클릭해서 공격 옵션을 보고 있는 중) 다시 채우지 않고 그대로 둔다.
        if (attackPanel != null && !attackPanel.IsShown)
            attackPanel.Show(selectedUnit, null, HandleAttackExecuted);
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

        LogUnitStatus(unit);
        RefreshSelectionDisplay();
    }


    private void DeselectUnit()
    {
        if (attackPanel != null)
            attackPanel.Hide();

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
