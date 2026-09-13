using UnityEngine;

/// <summary>
/// 아무 효과도 주지 않는 특성. "이 종족은 특별한 종족 보정이 없다"는 것을
/// 빈 배열이 아니라 명시적인 에셋으로 표현하기 위해 사용.
/// 인간의 실제 강점(넓은 무기 접근성)은 RaceData.availableWeapons에서 이미 표현되고 있음.
/// </summary>
[CreateAssetMenu(fileName = "Versatile", menuName = "Strategy/Traits/Versatile (No Effect)")]
public class VersatileTrait : RacialTrait
{
    // 모든 훅을 오버라이드하지 않음 -> 부모(RacialTrait)의 기본 동작(변경 없음)을 그대로 사용
}