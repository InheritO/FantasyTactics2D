using UnityEngine;

/// <summary>
/// 현재 선택된 유닛의 장비/스탯을 화면에 표시하는 디버그 도구.
/// 정식 UI가 만들어지면 제거해도 된다.
/// </summary>
public class UnitStatusDebugDisplay : MonoBehaviour
{
    public UnitSelectionController selectionController;

    void OnGUI()
    {
        UnitBase unit = selectionController.SelectedUnit;
        if (unit == null) return;

        string mainHand = unit.MainHandWeapon != null ? unit.MainHandWeapon.weaponName : "없음";
        string offHand = unit.OffHandWeapon != null ? unit.OffHandWeapon.weaponName : "없음";
        string shield = unit.EquippedShield != null ? unit.EquippedShield.shieldName : "없음";
        string armor = unit.EquippedArmor != null ? unit.EquippedArmor.armorName : "없음";

        string text = $"[{unit.name}]\n" +
                      $"주무기: {mainHand}\n" +
                      $"보조무기: {offHand}\n" +
                      $"방패: {shield}\n" +
                      $"방어구: {armor}\n" +
                      $"이동력: {unit.MoveRange}  사거리: {unit.AttackRange}\n" +
                      $"체력: {unit.CurrentHealth}/{unit.MaxHealth}\n" +
                      $"방어력: {unit.Defense} (맷집 {unit.ConstitutionDefense} + 장비 {unit.ArmorDefense})";

        GUI.Box(new Rect(10, 10, 250, 160), text);
    }
}