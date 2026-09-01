using UnityEngine;

/// <summary>
/// WASD 또는 방향키로 카메라를 이동시키는 간단한 스크립트.
/// 맵 확인용 프로토타입 단계 전용 (줌, 경계 제한 등은 포함하지 않음).
/// </summary>
public class SimpleCameraController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;

    void Update()
    {
        float horizontal = Input.GetAxisRaw("Horizontal"); // A/D, 좌우 방향키
        float vertical = Input.GetAxisRaw("Vertical");     // W/S, 상하 방향키

        Vector3 moveDir = new Vector3(horizontal, vertical, 0f).normalized;
        transform.position += moveDir * moveSpeed * Time.deltaTime;
    }
}