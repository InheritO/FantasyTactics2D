using UnityEngine;

/// <summary>
/// 유닛의 장비 슬롯(무기/방패/방어구)과 파괴 상태, 재장전 상태를 관리한다.
/// 순수 로직 클래스(MonoBehaviour 아님). UnitBase가 이 클래스를 소유하고 위임한다.
/// </summary>
public class UnitEquipmentState
{
    public WeaponData MainHandWeapon { get; private set; }
    public WeaponData OffHandWeapon { get; private set; }
    public ShieldData EquippedShield { get; private set; }
    public ArmorData EquippedArmor { get; private set; }

    // 방패 우선 파괴: 방패가 있으면 방패부터, 없거나 이미 부서졌으면 방어구가 파괴됨
    public bool ShieldBroken { get; private set; }
    public bool ArmorBroken { get; private set; }

    // 재장전이 필요한 무기의 현재 장전 상태
    public bool IsLoaded { get; private set; } = true;

    public bool EquipMainHandWeapon(WeaponData weapon)
    {
        if (weapon == null)
        {
            MainHandWeapon = null;
            return true;
        }

        if ((weapon.slotType & WeaponSlotType.MainHand) == 0)
        {
            Debug.Log($"{weapon.weaponName}은(는) 주 무기로 장착할 수 없습니다.");
            return false;
        }

        MainHandWeapon = weapon;

        // 양손 무기는 두 슬롯을 모두 차지, 방패와 공존 불가
        if (weapon.handedness == WeaponHandedness.TwoHanded)
        {
            OffHandWeapon = null;
            EquippedShield = null;
        }

        return true;
    }

    public bool EquipOffHandWeapon(WeaponData weapon)
    {
        if (weapon == null)
        {
            OffHandWeapon = null;
            return true;
        }

        if ((weapon.slotType & WeaponSlotType.OffHand) == 0)
        {
            Debug.Log($"{weapon.weaponName}은(는) 보조 무기로 장착할 수 없습니다.");
            return false;
        }

        if (MainHandWeapon != null && MainHandWeapon.handedness == WeaponHandedness.TwoHanded)
        {
            Debug.Log("양손 무기를 장착 중이라 보조 무기를 장착할 수 없습니다.");
            return false;
        }

        OffHandWeapon = weapon;

        // 보조무기와 방패는 같은 슬롯을 두고 경쟁 (UnitBase 규칙과 동일)
        EquippedShield = null;
        return true;
    }

    public bool EquipShield(ShieldData shield)
    {
        if (shield == null)
        {
            EquippedShield = null;
            return true;
        }

        if (MainHandWeapon != null && MainHandWeapon.handedness == WeaponHandedness.TwoHanded)
        {
            Debug.Log("양손 무기를 장착 중이라 방패를 장착할 수 없습니다.");
            return false;
        }

        EquippedShield = shield;
        OffHandWeapon = null;
        return true;
    }

    public void EquipArmor(ArmorData armor) => EquippedArmor = armor;

    // 방패가 있고 아직 안 부서졌으면 방패부터 파괴, 아니면 방어구를 파괴
    public void BreakEquipment()
    {
        if (EquippedShield != null && !ShieldBroken)
        {
            ShieldBroken = true;
            Debug.Log("방패가 파괴되었습니다!");
            return;
        }

        if (!ArmorBroken)
        {
            ArmorBroken = true;
            Debug.Log("방어구가 파괴되었습니다!");
        }
        // 방어구까지 이미 부서진 상태에서 또 발동하면 아무 일도 안 일어남 (자연스러움)
    }

    public void Reload() => IsLoaded = true;
    public void ConsumeAmmo() => IsLoaded = false;

    // 방어구 + 방패 보너스의 합. 파괴된 장비는 0으로 계산됨
    public int ArmorDefense =>
        (ArmorBroken ? 0 : (EquippedArmor?.defenseBonus ?? 0)) +
        (ShieldBroken ? 0 : (EquippedShield?.defenseBonus ?? 0));
}