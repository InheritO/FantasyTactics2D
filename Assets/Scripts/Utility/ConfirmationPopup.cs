using System;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(UIPopup))]
public class ConfirmationPopup : MonoBehaviour
{
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;

    private UIPopup popup;
    private Action onConfirmed;

    private void Awake()
    {
        popup = GetComponent<UIPopup>();
        confirmButton.onClick.AddListener(HandleConfirm);
        cancelButton.onClick.AddListener(HandleCancel);
    }

    private void OnDestroy()
    {
        confirmButton.onClick.RemoveListener(HandleConfirm);
        cancelButton.onClick.RemoveListener(HandleCancel);
    }

    public void Open(Action onConfirm)
    {
        onConfirmed = onConfirm;
        PopupCoordinator.Instance.Open(popup);
    }

    private void HandleConfirm()
    {
        popup.ClosePopup();
        onConfirmed?.Invoke();
        onConfirmed = null;
    }

    private void HandleCancel()
    {
        popup.ClosePopup();
        onConfirmed = null;
    }
}