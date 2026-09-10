using UnityEngine;
using UnityEngine.InputSystem.Controls;

/// <summary>
/// WASD 또는 방향키로 카메라를 이동시키는 간단한 스크립트.
/// 맵 확인용 프로토타입 단계 전용 (줌, 경계 제한 등은 포함하지 않음).
/// </summary>
public class SimpleCameraController : MonoBehaviour
{
    public float moveSpeed = 5f;

    private GameControls controls;

    void Awake()
    {
        controls = new GameControls();
    }

    void OnEnable() => controls.GamePlay.Enable();
    void OnDisable() => controls.GamePlay.Disable();

    void Update()
    {
        Vector2 moveInput = controls.GamePlay.Move.ReadValue<Vector2>();
        Vector3 moveDir = new Vector3(moveInput.x, moveInput.y, 0f).normalized;
        transform.position += moveDir * moveSpeed * Time.deltaTime;
    }
}