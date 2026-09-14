using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 부대 편성 화면의 UI 상호작용을 담당한다.
/// "현재 구성 중인 한 기(draft)"를 무기/방어구/방패 순환 버튼으로 조정하고,
/// "추가" 버튼으로 확정된 목록에 넣는 방식.
/// </summary>
public class RosterUIController : MonoBehaviour
{
    [Header("References")]
    public RosterPhaseManager rosterManager;

    [Header("Race Selection")]
    public Transform raceButtonContainer;
    public GameObject raceButtonPrefab; // Button + 자식에 TMP_Text

    [Header("Points Setting")]
    public Slider pointsSlider; // Unity UI의 Slider (min=50, max=200, step=10 정도로 설정)
    public TMP_Text pointsSettingLabel;

    [Header("AI Race Selection")]
    public Transform aiRaceButtonContainer;
    public GameObject aiRaceButtonPrefab;

    [Header("AI Strategy Selection")]
    public TMP_Dropdown aiStrategyDropdown;

    [Header("AI Selection Display")]
    public TMP_Text aiRaceLabel;

    [Header("AI Disposition Selection")]
    public TMP_Dropdown aiDispositionDropdown;

    [Header("Draft Controls")]
    public TMP_Text mainHandWeaponLabel;
    public Button mainHandWeaponNextButton;
    public TMP_Text offHandWeaponLabel;
    public Button offHandWeaponNextButton;
    public TMP_Text shieldLabel;
    public Button shieldNextButton;
    public TMP_Text armorLabel;
    public Button armorNextButton;
    public Button addUnitButton;

    [Header("Roster List")]
    public Transform rosterListContainer;
    public GameObject rosterListItemPrefab; // 자식에 TMP_Text + Button(제거)

    [Header("Points Display")]
    public TMP_Text pointsLabel;

    [Header("Confirm")]
    public Button confirmButton;

    private RosterEntry draft = new RosterEntry();
    private int mainHandIndex = -1;
    private int offHandIndex = -1;
    private int shieldIndex = -1;
    private int armorIndex = -1;

    private List<GameObject> spawnedListItems = new List<GameObject>();
    private bool isValid;


    void Start()
    {
        isValid = InspectorFieldValidator.ValidateAllFieldsAssigned(this);
        if (!isValid)
            return;

        BuildRaceButtons();
        BuildAIRaceButtons();
        SetupAIStrategyDropdown();
        SetupAIDispositionDropdown();

        mainHandWeaponNextButton.onClick.AddListener(CycleMainHandWeapon);
        offHandWeaponNextButton.onClick.AddListener(CycleOffHandWeapon);
        shieldNextButton.onClick.AddListener(CycleShield);
        armorNextButton.onClick.AddListener(CycleArmor);
        addUnitButton.onClick.AddListener(AddDraftToRoster);
        confirmButton.onClick.AddListener(rosterManager.ConfirmRoster);

        // 슬라이더 초기값을 현재 rosterManager 설정값으로 맞추고, 변경 이벤트 연결
        pointsSlider.value = rosterManager.totalPoints;
        pointsSlider.onValueChanged.AddListener(OnPointsSliderChanged);
        UpdatePointsSettingLabel();

        rosterManager.OnRosterChanged += RefreshUI;

        if (rosterManager.availableRaces.Length > 0 && rosterManager.availableRaces[0] != null)
            SelectRace(rosterManager.availableRaces[0]);
    }

    void OnDestroy()
    {
        if (!isValid)
            return;

        rosterManager.OnRosterChanged -= RefreshUI;

        pointsSlider.onValueChanged.RemoveListener(OnPointsSliderChanged);
        aiStrategyDropdown.onValueChanged.RemoveListener(OnAIStrategyChanged);
        aiDispositionDropdown.onValueChanged.RemoveListener(OnAIDispositionChanged);
    }

    private bool ValidateReferences()
    {
        var required = new (Object obj, string name)[]
   {
        (rosterManager, nameof(rosterManager)),
        (raceButtonContainer, nameof(raceButtonContainer)),
        (raceButtonPrefab, nameof(raceButtonPrefab)),
        (mainHandWeaponLabel, nameof(mainHandWeaponLabel)),
        (mainHandWeaponNextButton, nameof(mainHandWeaponNextButton)),
        (offHandWeaponLabel, nameof(offHandWeaponLabel)),
        (offHandWeaponNextButton, nameof(offHandWeaponNextButton)),
        (shieldLabel, nameof(shieldLabel)),
        (shieldNextButton, nameof(shieldNextButton)),
        (armorLabel, nameof(armorLabel)),
        (armorNextButton, nameof(armorNextButton)),
        (addUnitButton, nameof(addUnitButton)),
        (rosterListContainer, nameof(rosterListContainer)),
        (rosterListItemPrefab, nameof(rosterListItemPrefab)),
        (pointsLabel, nameof(pointsLabel)),
        (confirmButton, nameof(confirmButton)),
   };

        bool ok = true;

        foreach (var (obj, fieldName) in required)
        {
            if (obj == null)
            {
                Debug.LogError($"[{name}] {fieldName}이(가) 연결되지 않았습니다.");
                ok = false;
            }
        }

        if (ok && (rosterManager.availableRaces == null || rosterManager.availableRaces.Length == 0))
            Debug.LogWarning($"[{name}] rosterManager.availableRaces가 비어있습니다.");

        return ok;
    }

    private void BuildRaceButtons()
    {
        if (rosterManager.availableRaces == null)
            return;


        foreach (var race in rosterManager.availableRaces)
        {
            if (race == null)
            {
                Debug.LogWarning($"[{name}] availableRaces 배열에 빈 슬롯이 있습니다.");
                continue;
            }

            GameObject buttonObj = Instantiate(raceButtonPrefab, raceButtonContainer);

            TMP_Text label = buttonObj.GetComponentInChildren<TMP_Text>();
            if (label != null)
                label.text = race.raceName;
            else
                Debug.LogWarning($"[{name}] raceButtonPrefab에 TMP_Text 컴포넌트가 없습니다.");

            Button button = buttonObj.GetComponent<Button>();
            if (button != null)
                button.onClick.AddListener(() => SelectRace(race));
            else
                Debug.LogWarning($"[{name}] raceButtonPrefab에 Button 컴포넌트가 없습니다.");
        }
    }

    private void SelectRace(RaceData race)
    {
        if (race == null)
            return;

        rosterManager.SelectRace(race);
        ResetDraft();
        RefreshUI();
    }

    private void ResetDraft()
    {
        draft = new RosterEntry();
        mainHandIndex = -1;
        offHandIndex = -1;
        shieldIndex = -1;
        armorIndex = -1;
        RefreshDraftLabels();
    }

    private void CycleMainHandWeapon()
    {
        RaceData race = rosterManager.SelectedRace;
        if (race == null || race.availableWeapons == null || race.availableWeapons.Length == 0)
            return;

        var mainHandCandidates = System.Array.FindAll(race.availableWeapons, w => (w.slotType & WeaponSlotType.MainHand) != 0);
        if (mainHandCandidates.Length == 0)
            return;

        mainHandIndex++;
        if (mainHandIndex >= mainHandCandidates.Length)
            mainHandIndex = -1;

        draft.mainHandWeapon = mainHandIndex == -1 ? null : mainHandCandidates[mainHandIndex];

        // 양손 무기를 골랐으면 보조무기/방패는 UnitBase의 실제 장착 규칙과 동일하게 자동 해제
        // (이렇게 안 하면 나중에 실제 장착 시 한쪽만 적용되는데 비용은 둘 다 청구되는 문제가 생김)
        if (draft.mainHandWeapon != null && draft.mainHandWeapon.handedness == WeaponHandedness.TwoHanded)
        {
            draft.offHandWeapon = null;
            offHandIndex = -1;
            draft.shield = null;
            shieldIndex = -1;
        }

        RefreshDraftLabels();
    }

    private void CycleOffHandWeapon()
    {
        RaceData race = rosterManager.SelectedRace;
        if (race == null || race.availableWeapons == null || race.availableWeapons.Length == 0)
            return;


        if (draft.mainHandWeapon != null && draft.mainHandWeapon.handedness == WeaponHandedness.TwoHanded)
        {
            Debug.Log("양손 무기를 장착 중이라 보조 무기를 선택할 수 없습니다.");
            return;
        }

        var offHandCandidates = System.Array.FindAll(race.availableWeapons, w => (w.slotType & WeaponSlotType.OffHand) != 0);
        if (offHandCandidates.Length == 0)
            return;

        offHandIndex++;
        if (offHandIndex >= offHandCandidates.Length)
            offHandIndex = -1;

        draft.offHandWeapon = offHandIndex == -1 ? null : offHandCandidates[offHandIndex];

        // 보조무기와 방패는 같은 슬롯을 두고 경쟁 (UnitBase 규칙과 동일)
        if (draft.offHandWeapon != null)
        {
            draft.shield = null;
            shieldIndex = -1;
        }

        RefreshDraftLabels();
    }

    private void CycleShield()
    {
        RaceData race = rosterManager.SelectedRace;
        if (race == null || race.availableShields == null || race.availableShields.Length == 0)
            return;

        if (draft.mainHandWeapon != null && draft.mainHandWeapon.handedness == WeaponHandedness.TwoHanded)
        {
            Debug.Log("양손 무기를 장착 중이라 방패를 선택할 수 없습니다.");
            return;
        }

        shieldIndex++;
        if (shieldIndex >= race.availableShields.Length)
            shieldIndex = -1;

        draft.shield = shieldIndex == -1 ? null : race.availableShields[shieldIndex];

        if (draft.shield != null)
        {
            draft.offHandWeapon = null;
            offHandIndex = -1;
        }

        RefreshDraftLabels();
    }

    private void CycleArmor()
    {
        RaceData race = rosterManager.SelectedRace;
        if (race == null || race.availableArmors == null || race.availableArmors.Length == 0)
            return;

        armorIndex++;
        if (armorIndex >= race.availableArmors.Length)
            armorIndex = -1;

        draft.armor = armorIndex == -1 ? null : race.availableArmors[armorIndex];
        RefreshDraftLabels();
    }

    private void AddDraftToRoster()
    {
        if (rosterManager.SelectedRace == null)
        {
            Debug.Log("종족을 먼저 선택해주세요.");
            return;
        }


        RosterEntry newEntry = new RosterEntry
        {
            mainHandWeapon = draft.mainHandWeapon,
            offHandWeapon = draft.offHandWeapon,
            shield = draft.shield,
            armor = draft.armor
        };

        rosterManager.TryAddEntry(newEntry);
    }

    private void RefreshDraftLabels()
    {
        mainHandWeaponLabel.text = draft.mainHandWeapon != null ? draft.mainHandWeapon.weaponName : "비무장";
        offHandWeaponLabel.text = draft.offHandWeapon != null ? draft.offHandWeapon.weaponName : "없음";
        shieldLabel.text = draft.shield != null ? draft.shield.shieldName : "없음";
        armorLabel.text = draft.armor != null ? draft.armor.armorName : "비무장";

        bool isTwoHanded = draft.mainHandWeapon != null && draft.mainHandWeapon.handedness == WeaponHandedness.TwoHanded;
        offHandWeaponNextButton.interactable = !isTwoHanded;
        shieldNextButton.interactable = !isTwoHanded;
    }

    // 슬라이더 값이 바뀔 때마다 총 포인트를 갱신하고, 편성 목록/라벨을 새로 고침
    private void OnPointsSliderChanged(float value)
    {
        rosterManager.SetTotalPoints(Mathf.RoundToInt(value));
        UpdatePointsSettingLabel();
        // RefreshUI는 SetTotalPoints 내부의 SelectRace 호출이 OnRosterChanged를 발생시켜 자동으로 호출됨
    }

    private void UpdatePointsSettingLabel()
    {
        if (pointsSettingLabel != null)
            pointsSettingLabel.text = $"편성 포인트: {rosterManager.totalPoints}";
    }


    // 적이 사용할 종족 후보 버튼 생성 (플레이어 종족 선택과 같은 목록을 재사용)
    private void BuildAIRaceButtons()
    {
        if (rosterManager.availableRaces == null)
            return;

        foreach (var race in rosterManager.availableRaces)
        {
            if (race == null)
                continue;

            GameObject buttonObj = Instantiate(aiRaceButtonPrefab, aiRaceButtonContainer);

            TMP_Text label = buttonObj.GetComponentInChildren<TMP_Text>();
            if (label != null)
                label.text = race.raceName;

            Button button = buttonObj.GetComponent<Button>();
            if (button != null)
                button.onClick.AddListener(() => rosterManager.SelectAIRace(race));
        }
    }

    private void RefreshUI()
    {
        if (rosterManager.Builder != null)
            pointsLabel.text = $"포인트: {rosterManager.Builder.UsedPoints} / {rosterManager.Builder.TotalPoints}";

        if (aiRaceLabel != null)
            aiRaceLabel.text = $"적 종족: {(rosterManager.aiRace != null ? rosterManager.aiRace.raceName : "미선택")}";

        RebuildRosterList();
    }

    private void RebuildRosterList()
    {
        foreach (var item in spawnedListItems)
            Destroy(item);
        spawnedListItems.Clear();

        if (rosterManager.Builder == null)
            return;

        foreach (var entry in rosterManager.Builder.GetEntries())
        {
            GameObject itemObj = Instantiate(rosterListItemPrefab, rosterListContainer);
            spawnedListItems.Add(itemObj);

            TMP_Text label = itemObj.GetComponentInChildren<TMP_Text>();
            if (label != null)
                label.text = BuildEntryLabel(entry);

            Button removeButton = itemObj.GetComponentInChildren<Button>();
            if (removeButton != null)
                removeButton.onClick.AddListener(() => rosterManager.RemoveEntry(entry));
        }
    }

    private string BuildEntryLabel(RosterEntry entry)
    {
        string weapon = entry.mainHandWeapon != null ? entry.mainHandWeapon.weaponName : "비무장";
        string armor = entry.armor != null ? entry.armor.armorName : "비무장";
        int cost = entry.GetTotalCost(rosterManager.SelectedRace);
        return $"{weapon} / {armor} (비용 {cost})";
    }

    // 드롭다운 옵션을 enum 순서와 정확히 맞춰서 채우고, 현재 설정값을 초기 선택으로 표시
    private void SetupAIStrategyDropdown()
    {
        aiStrategyDropdown.ClearOptions();

        // AIRosterStrategy 순서: Standard=0, MeleeFocus=1, RangedFocus=2
        // 옵션 문자열 순서도 반드시 이 순서와 일치해야 함 (순서가 어긋나면 엉뚱한 전략이 선택됨)
        aiStrategyDropdown.AddOptions(new List<string> { "표준", "근거리 위주", "원거리 위주" });

        aiStrategyDropdown.value = (int)rosterManager.aiStrategy;
        aiStrategyDropdown.onValueChanged.AddListener(OnAIStrategyChanged);
    }

    private void SetupAIDispositionDropdown()
    {
        aiDispositionDropdown.ClearOptions();
        aiDispositionDropdown.AddOptions(new List<string> { "공격적", "방어적", "거리 유지" });

        aiDispositionDropdown.value = (int)rosterManager.aiDisposition;
        aiDispositionDropdown.onValueChanged.AddListener(OnAIDispositionChanged);
    }


    private void OnAIStrategyChanged(int index)
    {
        rosterManager.aiStrategy = (AIRosterStrategy)index;
    }

    private void OnAIDispositionChanged(int index)
    {
        rosterManager.SetAIDisposition((AICombatDisposition)index);
    }



}