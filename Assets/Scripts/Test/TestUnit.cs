using UnityEngine;

/// <summary>
/// 배치/이동 테스트용 임시 유닛.
/// 실제 게임 유닛(보병, 궁수 등)을 만들기 전 UnitBase의 기능 검증용.
/// </summary>
public class TestUnit : UnitBase
{
    // 지금은 이동/배치 테스트가 목적이라 특별한 행동 없이 로그만 출력
    public override void PerformAction()
    {
        Debug.Log($"{gameObject.name} performs a test action at {GridCoord}");
    }
}