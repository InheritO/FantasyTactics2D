using UnityEngine;

[CreateAssetMenu(fileName = "NewShield", menuName = "Strategy/Equipment/Shield")]
public class ShieldData : ScriptableObject
{
    public string shieldName;
    public Sprite icon;

    [Header("Visuals")]
    public CharacterAnimationSet visualSet;

    public int defenseBonus;
    public int moveRangePenalty;

    [Header("Block")]
    public int blockSkillBonus; // DefenseSkill에 더해지는 보너스 (고정 확률 아님)

    [Header("Roster Cost")]
    public int cost; // 방패는 아직 종족별 예외표가 없음. 필요해지면 RaceData에 같은 패턴으로 추가
}