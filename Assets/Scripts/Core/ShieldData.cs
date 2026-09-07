using UnityEngine;

[CreateAssetMenu(fileName = "NewShield", menuName = "Strategy/Equipment/Shield")]
public class ShieldData : ScriptableObject
{
    public string shieldName;
    public Sprite icon;
    public int defenseBonus;

    [Header("Roster Cost")]
    public int cost; // 방패는 아직 종족별 예외표가 없음. 필요해지면 RaceData에 같은 패턴으로 추가
}