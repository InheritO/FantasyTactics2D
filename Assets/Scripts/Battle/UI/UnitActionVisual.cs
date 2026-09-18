using System.Collections;
using UnityEngine;

/// <summary>
/// 유닛의 행동 상태(이동/공격 완료 여부)에 따라 스프라이트 색을 조정한다.
/// UnitSelectionController의 선택 하이라이트와는 별개로, "이번 턴에 더 행동 가능한가"를 표시한다.
/// 데미지를 받으면 잠깐 색이 번쩍였다가 원래 상태로 돌아오는 피드백도 함께 처리한다.
/// </summary>
public class UnitActionVisual : MonoBehaviour
{
    [Tooltip("데미지를 받았을 때 깜빡이는 색")]
    public Color damageFlashColor = Color.white;

    [Tooltip("깜빡임이 지속되는 시간(초)")]
    public float damageFlashDuration = 0.15f;

    private UnitBase unit;
    private SpriteRenderer spriteRenderer;
    private Color factionColor;
    private Coroutine flashRoutine;

    public void Initialize(UnitBase targetUnit, Color originalFactionColor)
    {
        unit = targetUnit;
        spriteRenderer = unit.GetComponent<SpriteRenderer>();
        factionColor = originalFactionColor;

        unit.OnActionsExhausted += HandleActionsExhausted;
        unit.OnTurnReset += HandleTurnReset;
        unit.OnDamaged += HandleDamaged; // 추가
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

    private void HandleDamaged(UnitBase u, int amount)
    {
        if (spriteRenderer == null)
            return;

        // 이미 깜빡이는 중이면 새로 시작 (연속 피격 시 자연스럽게 갱신됨)
        if (flashRoutine != null)
            StopCoroutine(flashRoutine);

        flashRoutine = StartCoroutine(FlashDamage());
    }

    private IEnumerator FlashDamage()
    {
        // 지금 색(행동완료로 어두워진 상태일 수도 있음)을 기준으로 삼아,
        // 깜빡임이 끝나면 "원래 상태"가 아니라 "지금 있어야 할 상태"로 복귀시켜야 함
        Color colorBeforeFlash = unit.CanStillAct ? factionColor : factionColor * 0.5f;

        spriteRenderer.color = damageFlashColor;

        yield return new WaitForSeconds(damageFlashDuration);

        spriteRenderer.color = colorBeforeFlash;
        flashRoutine = null;
    }

    void OnDestroy()
    {
        if (unit != null)
        {
            unit.OnActionsExhausted -= HandleActionsExhausted;
            unit.OnTurnReset -= HandleTurnReset;
            unit.OnDamaged -= HandleDamaged; // 추가
        }
    }
}