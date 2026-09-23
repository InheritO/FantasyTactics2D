using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class UILabledButton : MonoBehaviour
{
    [SerializeField]
    private TMP_Text label;

    [SerializeField]
    private Button button;

    public event Action OnClicked;

    void Awake() => button.onClick.AddListener(() => OnClicked?.Invoke());

    public void SetText(string text) => label.text = text;

    public void ButtonInteractable(bool _isEnable)
    {
        if (button == null)
            return;

        button.interactable = _isEnable;
    }
}
