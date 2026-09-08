/// <summary>
/// 부대 편성에서 "유닛 한 기 + 장착 장비" 한 세트를 표현한다.
/// 비용 계산은 RaceData를 거쳐서 계산한다 (종족별 예외표를 반영하기 위함).
/// </summary>
[System.Serializable]
public class RosterEntry
{
    public WeaponData mainHandWeapon;
    public WeaponData offHandWeapon;
    public ShieldData shield;
    public ArmorData armor;

    public int GetTotalCost(RaceData race)
    {
        if (race == null)
            return 0;

        int cost = race.unitCost;
        cost += race.GetWeaponCost(mainHandWeapon);
        cost += race.GetWeaponCost(offHandWeapon);
        cost += race.GetShieldCost(shield);
        cost += race.GetArmorCost(armor);
        return cost;
    }
}