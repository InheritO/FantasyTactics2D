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
    public Color damageFlashColor = Color.red;

    [Tooltip("깜빡임이 지속되는 시간(초)")]
    public float damageFlashDuration = 0.15f;

    [Tooltip("행동완료 시 밝기 배율 (1 = 원래 밝기)")]
    [Range(0f, 1f)] public float actionsExhaustedBrightness = 0.5f;

    private UnitBase unit;
    private SpriteRenderer spriteRenderer;
    private Coroutine flashRoutine;

    public void Initialize(UnitBase targetUnit, SpriteRenderer targetRenderer)
    {
        unit = targetUnit;
        spriteRenderer = targetRenderer;

        unit.OnActionsExhausted += HandleActionsExhausted;
        unit.OnTurnReset += HandleTurnReset;
        unit.OnDamaged += HandleDamaged;
    }

    private void HandleActionsExhausted(UnitBase u)
    {
        if (spriteRenderer != null)
            spriteRenderer.color = new Color(actionsExhaustedBrightness, actionsExhaustedBrightness, actionsExhaustedBrightness, 1f);
    }

    private void HandleTurnReset(UnitBase u)
    {
        if (spriteRenderer != null)
            spriteRenderer.color = Color.white;
    }

    private void HandleDamaged(UnitBase u, int amount)
    {
        if (spriteRenderer == null)
            return;

        if (flashRoutine != null)
            StopCoroutine(flashRoutine);

        flashRoutine = StartCoroutine(FlashDamage());
    }

    private IEnumerator FlashDamage()
    {
        // 지금 색(행동완료로 어두워진 상태일 수도 있음)을 기준으로 삼아,
        // 깜빡임이 끝나면 "원래 상태"가 아니라 "지금 있어야 할 상태"로 복귀시켜야 함
        Color colorBeforeFlash = unit.CanStillAct
            ? Color.white
            : new Color(actionsExhaustedBrightness, actionsExhaustedBrightness, actionsExhaustedBrightness, 1f);

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
            unit.OnDamaged -= HandleDamaged;
        }
    }
}