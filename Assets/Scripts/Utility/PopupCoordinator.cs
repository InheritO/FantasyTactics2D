using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 등록된 팝업들의 상호배제(하나 열리면 나머지 닫기)와,
/// "지금 하나라도 열려있는지" 상태를 함께 관리한다.
/// 이 값은 게임플레이 단축키를 막을지 판단하는 데 쓰인다 (UnitSelectionController 참고).
/// </summary>
public class PopupCoordinator : MonoBehaviour
{
    public static PopupCoordinator Instance { get; private set; }

    public GameObject popupLayer;
    public List<UIPopup> popups;
    public CanvasGroup backdrop; // PopupLayer 전체가 아니라 백드롭 이미지의 CanvasGroup

    private int openCount;
    public static bool IsAnyPopupOpen { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError($"[{name}] PopupCoordinator가 씬에 이미 하나 더 있습니다. 하나만 있어야 해서 이 오브젝트는 제거합니다.");
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (popupLayer != null)
            popupLayer.SetActive(true);

        foreach (var popup in popups)
        {
            popup.gameObject.SetActive(true);
        }


        if (popupLayer != null)
            popupLayer.SetActive(false);

        SetBackdropVisible(false);

        foreach (var popup in popups)
        {
            popup.gameObject.SetActive(true);
        }
    }

    private void OnEnable()
    {
        foreach (var popup in popups)
        {
            popup.OnOpened += HandlePopupOpened;
            popup.OnClosed += HandlePopupClosed;
        }
    }

    private void OnDisable()
    {
        foreach (var popup in popups)
        {
            popup.OnOpened -= HandlePopupOpened;
            popup.OnClosed -= HandlePopupClosed;
        }
    }

    public void Open(UIPopup popup)
    {
        if (popupLayer != null)
            popupLayer.SetActive(true); // 자식의 Awake/코루틴이 돌 수 있게 부모부터 켬

        popup.OpenPopup();
    }

    private void HandlePopupOpened(UIPopup opened)
    {
        openCount++;
        IsAnyPopupOpen = true;
        SetBackdropVisible(true);

        foreach (var popup in popups)
            if (popup != opened)
                popup.ClosePopup();
    }

    private void HandlePopupClosed(UIPopup closed)
    {
        openCount = Mathf.Max(0, openCount - 1);
        IsAnyPopupOpen = openCount > 0;

        if (openCount == 0)
        {
            SetBackdropVisible(false);

            if (popupLayer != null)
                popupLayer.SetActive(false); // 다 닫히면 부모도 다시 꺼서 클릭 차단 해제
        }
    }

    private void SetBackdropVisible(bool visible)
    {
        if (backdrop == null)
            return;

        backdrop.alpha = visible ? 1f : 0f;
        backdrop.blocksRaycasts = visible;
    }
}