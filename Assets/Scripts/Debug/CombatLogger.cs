using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 전투 관련 이벤트를 구독해서 콘솔에 로그로 출력하는 디버그 도구.
/// 실제 UI/사운드/이펙트가 만들어지면 이 클래스는 참고만 하고 대체될 수 있음.
/// </summary>
public class CombatLogger : MonoBehaviour
{
    [Header("UI (비워두면 콘솔에만 출력)")]
    public TMP_Text logText;
    public ScrollRect scrollRect;

    private const int MaxLines = 50;
    private readonly List<string> lines = new List<string>();

    public void RegisterUnit(UnitBase unit)
    {
        unit.OnMoved += HandleMoved;
        unit.OnAttackPerformed += HandleAttackPerformed;
        unit.OnAttackResult += HandleAttackResult;
        unit.OnDamaged += HandleDamaged;
        unit.OnDied += HandleDied;
        unit.OnActionsExhausted += HandleActionsExhausted;
    }

    private void AppendLine(string line)
    {
        Debug.Log(line);

        lines.Add(line);
        if (lines.Count > MaxLines)
            lines.RemoveAt(0);

        if (logText != null)
        {
            logText.text = string.Join("\n", lines);
            StartCoroutine(ScrollToBottomNextFrame());
        }
    }

    private IEnumerator ScrollToBottomNextFrame()
    {
        yield return null; // 스크롤뷰 때와 같은 이유 - 레이아웃이 갱신될 시간을 줌
        Canvas.ForceUpdateCanvases();
        if (scrollRect != null)
            scrollRect.verticalNormalizedPosition = 0f; // 0 = 맨 아래, 최신 로그가 보이게
    }

    private void HandleMoved(UnitBase unit, Vector2Int from, Vector2Int to) =>
        AppendLine($"[{unit.name}] 이동: {from} → {to}");

    private void HandleAttackPerformed(UnitBase attacker, UnitBase target) =>
        AppendLine($"[{attacker.name}]이(가) {target.name}을(를) 공격합니다.");

    private void HandleAttackResult(UnitBase attacker, UnitBase target, CombatResult result) =>
        AppendLine(result.IsHit ? $"  → 명중! {result.DamageDealt} 데미지." : "  → 빗나감.");

    private void HandleDamaged(UnitBase unit, int amount) =>
        AppendLine($"[{unit.name}] {amount} 데미지 받음. 남은 체력: {unit.CurrentHealth}/{unit.MaxHealth}");

    private void HandleDied(UnitBase unit) =>
        AppendLine($"[{unit.name}] 사망.");

    private void HandleActionsExhausted(UnitBase unit) =>
        AppendLine($"[{unit.name}] 이번 턴에 더 이상 행동할 수 없습니다.");
}