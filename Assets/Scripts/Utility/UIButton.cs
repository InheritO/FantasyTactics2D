using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Button.onClick을 감싸서 C# 이벤트(OnClicked)로 재노출하고,
/// 클릭 시 공용 클릭음(SfxLibrary.buttonClick)을 자동으로 재생한다.
/// </summary>
[RequireComponent(typeof(Button))]
public class UIButton : MonoBehaviour
{
    [SerializeField] private Button button;

    public event Action OnClicked;

    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();

        button.onClick.AddListener(HandleClicked);
    }

    private void OnDestroy()
    {
        button.onClick.RemoveListener(HandleClicked);
    }

    private void HandleClicked()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySfx(AudioManager.Instance.library.buttonClick);

        OnClicked?.Invoke();
    }
}