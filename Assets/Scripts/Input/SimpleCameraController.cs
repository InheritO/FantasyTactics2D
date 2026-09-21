using UnityEngine;
using UnityEngine.InputSystem.Controls;

/// <summary>
/// WASD 또는 방향키로 카메라를 이동시키는 간단한 스크립트.
/// 맵 확인용 프로토타입 단계 전용 (줌, 경계 제한 등은 포함하지 않음).
/// </summary>
public class SimpleCameraController : MonoBehaviour
{
    [Header("Reference")]
    public BattlePhaseManager phaseManager;

    [Header("Value")]
    public float moveSpeed = 5f;

    [Header("Zoom")]
    public float zoomSpeed = 5f;
    public float minZoom = 3f;
    public float maxZoom = 15f;


    private GameControls controls;
    private Camera cam;

    void Awake()
    {
        controls = new GameControls();
        InputBindingUtility.LoadOverrides(controls.asset);

        cam = GetComponent<Camera>();

        if (cam == null)
            Debug.LogError($"[{name}] Camera 컴포넌트를 찾을 수 없습니다.");
    }

    void OnEnable() => controls.GamePlay.Enable();
    void OnDisable() => controls.GamePlay.Disable();

    private void Update()
    {
        if (phaseManager == null)
            return;

        bool isGridVisible = phaseManager.CurrentPhase == BattlePhase.Placement
            || phaseManager.CurrentPhase == BattlePhase.Battle;

        if (!isGridVisible)
            return;

        Vector2 moveInput = controls.GamePlay.Move.ReadValue<Vector2>();
        Vector3 moveDir = new Vector3(moveInput.x, moveInput.y, 0f).normalized;
        transform.position += moveDir * moveSpeed * Time.deltaTime;

        HandleZoom();
    }

    private void HandleZoom()
    {
        if (cam == null || !cam.orthographic)
            return;

        Vector2 scroll = controls.GamePlay.Zoom.ReadValue<Vector2>();


        if (Mathf.Approximately(scroll.y, 0f))
            return;

        cam.orthographicSize -= scroll.y * zoomSpeed * Time.deltaTime;
        cam.orthographicSize = Mathf.Clamp(cam.orthographicSize, minZoom, maxZoom);
    }
}