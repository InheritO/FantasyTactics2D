using UnityEngine;

/// <summary>
/// 테스트 목적으로 세력, 무기, 방어구를 순환 선택하고 클릭한 위치에 배치하는 도구.
/// Tab: 세력 전환, Q: 무기 전환, E: 방어구 전환, 우클릭: 배치
/// </summary>
public class TestUnitPlacer : MonoBehaviour
{
    [Header("References")]
    public GridManager gridManager;
    public BattlePhaseManager phaseManager;
    public BattleOutcomeManager outcomeManager;
    public TestUnit testUnitPrefab;
    public FactionData[] factions;

    private int currentFactionIndex = 0;
    private int currentWeaponIndex = -1; // -1 = 비무장
    private int currentArmorIndex = -1;  // -1 = 비무장

    void Update()
    {
        if (phaseManager.CurrentPhase != BattlePhase.Placement)
            return;

        if (Input.GetKeyDown(KeyCode.Tab))
            CycleFaction();

        if (Input.GetKeyDown(KeyCode.Q))
            CycleWeapon();

        if (Input.GetKeyDown(KeyCode.E))
            CycleArmor();

        if (Input.GetMouseButtonDown(1))
            PlaceUnitAtMouse();
    }

    private void CycleFaction()
    {
        if (factions.Length == 0) return;

        currentFactionIndex = (currentFactionIndex + 1) % factions.Length;
        currentWeaponIndex = -1; // 세력이 바뀌면 장비 풀이 달라지니 초기화
        currentArmorIndex = -1;

        Debug.Log($"현재 배치 세력: {factions[currentFactionIndex].factionName}");
    }

    private void CycleWeapon()
    {
        RaceData race = factions[currentFactionIndex].race;
        if (race == null || race.availableWeapons.Length == 0)
        {
            Debug.Log("이 종족은 사용 가능한 무기가 없습니다.");
            return;
        }

        // -1(비무장)부터 시작해서 순환
        currentWeaponIndex++;
        if (currentWeaponIndex >= race.availableWeapons.Length)
            currentWeaponIndex = -1;

        string weaponName = currentWeaponIndex == -1 ? "비무장" : race.availableWeapons[currentWeaponIndex].weaponName;
        Debug.Log($"현재 무기: {weaponName}");
    }

    private void CycleArmor()
    {
        RaceData race = factions[currentFactionIndex].race;
        if (race == null || race.availableArmors.Length == 0)
        {
            Debug.Log("이 종족은 사용 가능한 방어구가 없습니다.");
            return;
        }

        currentArmorIndex++;
        if (currentArmorIndex >= race.availableArmors.Length)
            currentArmorIndex = -1;

        string armorName = currentArmorIndex == -1 ? "비무장" : race.availableArmors[currentArmorIndex].armorName;
        Debug.Log($"현재 방어구: {armorName}");
    }

    private void PlaceUnitAtMouse()
    {
        if (factions.Length == 0)
        {
            Debug.Log("등록된 세력이 없습니다.");
            return;
        }

        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0f;
        Vector2Int coord = gridManager.WorldToGrid(mouseWorldPos);

        FactionData selectedFaction = factions[currentFactionIndex];
        UnitBase unit = UnitSpawner.Spawn(testUnitPrefab, coord, selectedFaction, gridManager, outcomeManager);

        if (unit == null)
            return;

        RaceData race = selectedFaction.race;

        if (race != null && currentWeaponIndex >= 0 && currentWeaponIndex < race.availableWeapons.Length)
            unit.EquipMainHandWeapon(race.availableWeapons[currentWeaponIndex]);

        if (race != null && currentArmorIndex >= 0 && currentArmorIndex < race.availableArmors.Length)
            unit.EquipArmor(race.availableArmors[currentArmorIndex]);
    }
}