using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class UIPopup : MonoBehaviour
{
    [SerializeField] private Button closeButton;
    [SerializeField] private float animationDuration = 0.1f;

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Coroutine animationCoroutine;

    private bool initialized;

    public event Action<UIPopup> OnOpened;
    public event Action<UIPopup> OnClosed;

    public bool IsOpen { get; private set; }

    private void Awake()
    {
        EnsureInitialized();
    }

    private void EnsureInitialized()
    {
        if (initialized)
            return;

        initialized = true;

        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();

        SetClosedImmediate();

        if (closeButton != null)
            closeButton.onClick.AddListener(ClosePopup);
    }

    private void OnDestroy()
    {
        if (closeButton != null)
            closeButton.onClick.RemoveListener(ClosePopup);
    }

    private void SetClosedImmediate()
    {
        IsOpen = false;
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        rectTransform.localScale = Vector3.zero;
    }

    public void OpenPopup()
    {
        EnsureInitialized();

        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        if (IsOpen)
            return;

        IsOpen = true;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        OnOpened?.Invoke(this);

        if (animationCoroutine != null)
            StopCoroutine(animationCoroutine);

        animationCoroutine = StartCoroutine(AnimateCoroutine(true));
    }

    public void ClosePopup()
    {
        if (!IsOpen)
            return;

        IsOpen = false;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        if (animationCoroutine != null)
            StopCoroutine(animationCoroutine);
        animationCoroutine = StartCoroutine(AnimateCoroutine(false));
    }

    private IEnumerator AnimateCoroutine(bool opening)
    {
        Vector3 startScale = rectTransform.localScale;
        Vector3 targetScale = opening ? Vector3.one : Vector3.zero;
        float startAlpha = canvasGroup.alpha;
        float targetAlpha = opening ? 1f : 0f;
        float elapsed = 0f;

        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / animationDuration);
            rectTransform.localScale = Vector3.Lerp(startScale, targetScale, t);
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            yield return null;
        }

        rectTransform.localScale = targetScale;
        canvasGroup.alpha = targetAlpha;
        animationCoroutine = null;

        if (!opening)
            OnClosed?.Invoke(this); // 다 닫힌 뒤 발동, 기존 규칙 그대로 유지
    }
}