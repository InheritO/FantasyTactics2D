using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// EndTurn / Cancel / ToggleActions 세 개의 단일 키 액션만 재바인딩을 지원한다.
/// </summary>
public class KeyRebindController : MonoBehaviour
{
    [System.Serializable]
    public struct RebindRow
    {
        public string actionName; // GameControls 액션 이름과 정확히 일치 (EndTurn / Cancel / ToggleActions)
        public Button rebindButton;
        public TextMeshProUGUI currentBindingLabel;
    }

    public RebindRow[] rows;

    private GameControls controls;
    private InputActionRebindingExtensions.RebindingOperation activeRebind;

    void Awake()
    {
        controls = new GameControls();
        InputBindingUtility.LoadOverrides(controls.asset);
    }

    void OnEnable()
    {
        RefreshAllLabels();

        foreach (var row in rows)
        {
            var capturedRow = row; // 클로저가 루프 변수를 그대로 참조하는 문제 방지
            row.rebindButton.onClick.AddListener(() => StartRebind(capturedRow));
        }
    }

    void OnDisable()
    {
        activeRebind?.Cancel();

        foreach (var row in rows)
            row.rebindButton.onClick.RemoveAllListeners();
    }

    private void StartRebind(RebindRow row)
    {
        InputAction action = controls.asset.FindAction(row.actionName);
        if (action == null)
        {
            Debug.LogWarning($"KeyRebindController: 액션 '{row.actionName}'을 찾을 수 없습니다.");
            return;
        }

        row.currentBindingLabel.text = "키 입력 대기중...";
        action.Disable();

        activeRebind = action.PerformInteractiveRebinding()
            .WithControlsExcluding("Mouse")
            .OnMatchWaitForAnother(0.1f)
            .OnComplete(operation => FinishRebind(row, action, operation))
            .OnCancel(operation => FinishRebind(row, action, operation))
            .Start();
    }

    private void FinishRebind(RebindRow row, InputAction action, InputActionRebindingExtensions.RebindingOperation operation)
    {
        operation.Dispose();
        action.Enable();

        row.currentBindingLabel.text = action.GetBindingDisplayString();
        InputBindingUtility.SaveOverrides(controls.asset);
    }

    private void RefreshAllLabels()
    {
        foreach (var row in rows)
        {
            InputAction action = controls.asset.FindAction(row.actionName);
            if (action != null)
                row.currentBindingLabel.text = action.GetBindingDisplayString();
        }
    }
}