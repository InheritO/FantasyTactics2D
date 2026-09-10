using System;

/// <summary>
/// 유닛이 선택된 대상에게 실행할 수 있는 하나의 공격 방식.
/// 지금은 항상 "공격" 하나뿐이지만, 나중에 무기가 여러 공격 방식(찌르기/베기 등)을
/// 갖게 되면 AttackOptionProvider에서 옵션만 늘리면 되고 UI는 수정할 필요가 없다.
/// </summary>
public struct AttackOption
{
    public string Label;
    public Action Execute;
}