using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 같은 오브젝트의 UIPopup이 열릴 때마다 지정된 ScrollRect를 맨 위로 되돌린다.
/// 스크롤뷰가 있는 팝업에만 붙이면 되고, UIPopup 자체는 스크롤뷰의 존재를 몰라도 된다.
/// </summary>
[RequireComponent(typeof(UIPopup))]
public class ScrollResetOnOpen : MonoBehaviour
{
    [SerializeField] private ScrollRect scrollRect;

    private UIPopup popup;

    private void Awake()
    {
        popup = GetComponent<UIPopup>();
    }

    private void OnEnable()
    {
        popup.OnOpened += HandleOpened;
    }

    private void OnDisable()
    {
        popup.OnOpened -= HandleOpened;
    }

    private void HandleOpened(UIPopup _)
    {
        StartCoroutine(ResetScrollNextFrame());
    }

    private IEnumerator ResetScrollNextFrame()
    {
        yield return null; // 레이아웃이 완전히 갱신될 시간을 한 프레임 줌
        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 1f;
    }
}