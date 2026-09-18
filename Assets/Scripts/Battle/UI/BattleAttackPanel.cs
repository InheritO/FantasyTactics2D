using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 선택된 유닛이 대상에게 실행할 수 있는 공격 방식들을 버튼 목록으로 보여준다.
/// </summary>
public class BattleAttackPanel : MonoBehaviour
{
    public GameObject panelRoot;
    public Transform optionButtonContainer;
    public GameObject optionButtonPrefab; // Button + TMP_Text

    private List<GameObject> spawnedButtons = new List<GameObject>();
    private List<AttackOption> currentOptions = new List<AttackOption>();
    private Action currentOnOptionExecuted;

    // 지금 패널이 화면에 열려 있는지. UnitSelectionController가 토글(열기/닫기) 판단에 사용한다.
    public bool IsShown => panelRoot != null && panelRoot.activeSelf;


    private static readonly Key[] NumberKeys =
{
    Key.Digit1, Key.Digit2, Key.Digit3,
    Key.Digit4, Key.Digit5, Key.Digit6,
    Key.Digit7, Key.Digit8, Key.Digit9
};


    void Awake()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    void Update()
    {
        if (panelRoot == null || !panelRoot.activeSelf)
            return;

        if (Keyboard.current == null)
            return;

        for (int i = 0; i < currentOptions.Count && i < NumberKeys.Length; i++)
        {
            if (Keyboard.current[NumberKeys[i]].wasPressedThisFrame)
            {
                ExecuteOption(currentOptions[i]);
                return;
            }
        }
    }

    public void Show(UnitBase attacker, UnitBase target, Action onOptionExecuted)
    {
        Clear();

        currentOptions = AttackOptionProvider.GetOptions(attacker, target);
        currentOnOptionExecuted = onOptionExecuted;

        if (currentOptions.Count == 0)
        {
            Hide();
            return;
        }

        for (int i = 0; i < currentOptions.Count; i++)
        {
            AttackOption option = currentOptions[i];

            GameObject buttonObj = Instantiate(optionButtonPrefab, optionButtonContainer);
            spawnedButtons.Add(buttonObj);

            TMP_Text label = buttonObj.GetComponentInChildren<TMP_Text>();
            if (label != null)
            {
                // 단축키 숫자를 라벨에 같이 표시 (예: "1. 베기")
                string keyHint = (i < NumberKeys.Length) ? $"{i + 1}. " : "";
                label.text = keyHint + option.Label;
            }

            Button button = buttonObj.GetComponent<Button>();
            if (button != null)
                button.onClick.AddListener(() => ExecuteOption(option));

            buttonObj.SetActive(true);
        }

        if (panelRoot != null)
            panelRoot.SetActive(true);
    }

    private void ExecuteOption(AttackOption option)
    {
        option.Execute?.Invoke();
        currentOnOptionExecuted?.Invoke();
        Hide();
    }

    public void Hide()
    {
        Clear();
        currentOptions.Clear();
        currentOnOptionExecuted = null;

        if (panelRoot != null)
            panelRoot.SetActive(false);
    }
    private void Clear()
    {
        foreach (var obj in spawnedButtons)
            Destroy(obj);
        spawnedButtons.Clear();
    }
}
