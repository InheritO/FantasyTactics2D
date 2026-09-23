using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RemovableListItem : MonoBehaviour
{
    [SerializeField]
    public TMP_Text label;      // 배경 위에 항목 정보를 보여주는 텍스트

    [SerializeField]
    public Button removeButton; // 버튼 자체의 텍스트("제거" 등)는 프리팹에 고정이라 별도 참조 불필요

    public event Action OnRemoveClicked;

    void Awake() => removeButton.onClick.AddListener(() => OnRemoveClicked?.Invoke());

    public void SetText(string text) => label.text = text;
}