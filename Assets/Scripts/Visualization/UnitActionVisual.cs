using UnityEngine;

/// <summary>
/// 유닛의 행동 상태(이동/공격 완료 여부)에 따라 스프라이트 색을 조정한다.
/// UnitSelectionController의 선택 하이라이트와는 별개로, "이번 턴에 더 행동 가능한가"를 표시한다.
/// </summary>
public class UnitActionVisual : MonoBehaviour
{
    private UnitBase unit;
    private SpriteRenderer spriteRenderer;
    private Color factionColor;

    public void Initialize(UnitBase targetUnit, Color originalFactionColor)
    {
        unit = targetUnit;
        spriteRenderer = unit.GetComponent<SpriteRenderer>();
        factionColor = originalFactionColor;

        unit.OnActionsExhausted += HandleActionsExhausted;
        unit.OnTurnReset += HandleTurnReset; // 아래에서 UnitBase에 이 이벤트를 추가할 예정
    }

    private void HandleActionsExhausted(UnitBase u)
    {
        if (spriteRenderer != null)
            spriteRenderer.color = factionColor * 0.5f;
    }

    private void HandleTurnReset(UnitBase u)
    {
        if (spriteRenderer != null)
            spriteRenderer.color = factionColor;
    }

    void OnDestroy()
    {
        if (unit != null)
        {
            unit.OnActionsExhausted -= HandleActionsExhausted;
            unit.OnTurnReset -= HandleTurnReset;
        }
    }
}