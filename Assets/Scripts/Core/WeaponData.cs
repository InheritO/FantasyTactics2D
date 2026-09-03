using UnityEngine;

public enum DamageScaling
{
    Strength,   // 힘에 비례 (검, 창, 활, 투척무기)
    Fixed       // 힘과 무관, 무기 자체 위력이 곧 데미지 (석궁, 총)
}

[CreateAssetMenu(fileName = "NewWeapon", menuName = "Strategy/Equipment/Weapon")]
public class WeaponData : ScriptableObject
{
    public string weaponName;
    public bool isRanged;
    public int basePower;              // 무기 자체의 기본 위력
    public DamageScaling damageScaling; // 힘 반영 여부
    public int attackRangeOverride = -1;

}