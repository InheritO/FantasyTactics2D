using UnityEngine;

/// <summary>
/// 방어태세(Steady) 진입 행동. 무기와 무관하게 항상 선택 가능한 "공용 행동"
/// (UnitBase.UniversalActions)으로 등록해서 사용한다. 자기 자신에게 적용되므로
/// target은 사용하지 않는다.
/// </summary>
[CreateAssetMenu(fileName = "Steady", menuName = "Strategy/Actions/Stand Ground (Steady)")]
public class StandGroundAction : UnitAction
{
    // 에셋을 새로 만들 때(Create 메뉴) 기본 표시 이름을 자동으로 채워준다
    private void Reset()
    {
        actionName = "방어태세";
    }

    public override bool IsAvailable(UnitBase actor, UnitBase target)
    {
        if (actor == null)
            return false;

        // 이미 방어태세 중이거나 남은 행동력이 없으면 다시 선택할 필요 없음
        return !actor.IsBraced && actor.HasActionsRemaining;
    }

    public override void Execute(UnitBase actor, UnitBase target)
    {
        actor?.EnterBracedStance();
    }
}
