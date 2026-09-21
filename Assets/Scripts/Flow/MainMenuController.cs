using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 메인 메뉴 진입점. 게임 시작/옵션/플레이 방법/종료 버튼을 처리한다.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [SerializeField] private Button startButton;
    [SerializeField] private Button optionsButton;
    [SerializeField] private Button guideButton;
    [SerializeField] private Button quitButton;

    public GameObject mainMenuRoot;
    public SkirmishFlowController skirmishFlow;
    public UIPopup optionsPopup;
    public UIPopup howToPlayPopup;

    private void Awake()
    {
        mainMenuRoot.SetActive(true);

        startButton.onClick.AddListener(OnStartClicked);
        optionsButton.onClick.AddListener(OnOptionsClicked);
        guideButton.onClick.AddListener(OnGuideClicked);
        quitButton.onClick.AddListener(OnQuitClicked);
    }

    private void OnDestroy()
    {
        startButton.onClick.RemoveListener(OnStartClicked);
        optionsButton.onClick.RemoveListener(OnOptionsClicked);
        guideButton.onClick.RemoveListener(OnGuideClicked);
        quitButton.onClick.RemoveListener(OnQuitClicked);
    }

    private void OnStartClicked()
    {
        mainMenuRoot.SetActive(false);
        skirmishFlow.StartSkirmish();
    }

    private void OnOptionsClicked() => PopupCoordinator.Instance.Open(optionsPopup);
    private void OnGuideClicked() => PopupCoordinator.Instance.Open(howToPlayPopup);

    public void Show() => mainMenuRoot.SetActive(true);

    private void OnQuitClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}