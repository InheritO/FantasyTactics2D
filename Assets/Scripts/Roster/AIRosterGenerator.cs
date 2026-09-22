using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum AIRosterStrategy
{
    Standard,
    MeleeFocus,
    RangedFocus
}

/// <summary>
/// AI 세력의 부대를 무작위로 편성한다.
/// 규칙: 모든 유닛은 반드시 무기를 하나 장착한다 (비무장 유닛 금지).
/// 그 외 보조무기/방패/방어구 조합은 무작위로 채운다.
/// strategy에 따라 근접/원거리 무기 선택 비중이 달라진다.
/// </summary>
public static class AIRosterGenerator
{
    private const int MaxAttempts = 200; // 포인트가 애매하게 남았을 때 무한 루프 방지용 상한
    private const float FocusedWeightRatio = 0.8f; // 선호 무기 종류가 뽑힐 확률


    public static List<RosterEntry> GenerateRoster(RaceData race, int totalPoints, AIRosterStrategy strategy = AIRosterStrategy.Standard)
    {
        List<RosterEntry> result = new List<RosterEntry>();

        if (race == null)
        {
            Debug.LogWarning("AIRosterGenerator: race가 null입니다.");
            return result;
        }

        WeaponData[] mainHandCandidates = (race.availableWeapons ?? new WeaponData[0])
            .Where(w => w != null && (w.slotType & WeaponSlotType.MainHand) != 0)
            .ToArray();

        if (mainHandCandidates.Length == 0)
        {
            Debug.LogWarning($"AIRosterGenerator: {race.raceName}에 주손 장착 가능한 무기가 없어 부대를 편성할 수 없습니다.");
            return result;
        }

        WeaponData[] offHandCandidates = (race.availableWeapons ?? new WeaponData[0])
            .Where(w => w != null && (w.slotType & WeaponSlotType.OffHand) != 0)
            .ToArray();

        ArmorData[] armorCandidates = race.availableArmors ?? new ArmorData[0];
        ShieldData[] shieldCandidates = race.availableShields ?? new ShieldData[0];

        int cheapestWeaponCost = mainHandCandidates.Min(w => race.GetWeaponCost(w));
        int minPossibleCost = race.unitCost + cheapestWeaponCost;

        RosterBuilder builder = new RosterBuilder(race, totalPoints);
        int attempts = 0;

        while (attempts < MaxAttempts && builder.RemainingPoints >= minPossibleCost)
        {
            attempts++;

            RosterEntry entry = CreateRandomEntry(mainHandCandidates, offHandCandidates, armorCandidates, shieldCandidates, strategy, race.unarmoredArmor);
            int cost = entry.GetTotalCost(race);

            if (cost <= builder.RemainingPoints)
                builder.TryAddEntry(entry);
        }

        return new List<RosterEntry>(builder.GetEntries());
    }

    private static RosterEntry CreateRandomEntry(WeaponData[] mainHandCandidates, WeaponData[] offHandCandidates,
    ArmorData[] armorCandidates, ShieldData[] shieldCandidates, AIRosterStrategy strategy, ArmorData unarmoredArmor)
    {
        RosterEntry entry = new RosterEntry();

        entry.mainHandWeapon = PickMainHandWeapon(mainHandCandidates, strategy);

        bool isTwoHanded = entry.mainHandWeapon.handedness == WeaponHandedness.TwoHanded;

        if (!isTwoHanded)
        {
            int choice = Random.Range(0, 3);
            if (choice == 1 && offHandCandidates.Length > 0)
                entry.offHandWeapon = offHandCandidates[Random.Range(0, offHandCandidates.Length)];
            else if (choice == 2 && shieldCandidates.Length > 0)
                entry.shield = shieldCandidates[Random.Range(0, shieldCandidates.Length)];
        }

        int armorIndex = Random.Range(-1, armorCandidates.Length);
        entry.armor = armorIndex >= 0 ? armorCandidates[armorIndex] : unarmoredArmor;

        return entry;
    }

    // 전략에 따라 근접/원거리 무기 중 어느 쪽을 더 자주 고를지 결정
    private static WeaponData PickMainHandWeapon(WeaponData[] candidates, AIRosterStrategy strategy)
    {
        if (strategy == AIRosterStrategy.Standard)
            return candidates[Random.Range(0, candidates.Length)];

        bool preferRanged = strategy == AIRosterStrategy.RangedFocus;

        WeaponData[] preferred = candidates.Where(w => w.isRanged == preferRanged).ToArray();
        WeaponData[] fallback = candidates.Where(w => w.isRanged != preferRanged).ToArray();

        // 선호하는 종류가 없으면(해당 종족이 그 종류 무기를 아예 못 씀) 남은 후보에서 선택
        if (preferred.Length == 0)
            return fallback[Random.Range(0, fallback.Length)];

        if (fallback.Length == 0)
            return preferred[Random.Range(0, preferred.Length)];

        bool useFallback = Random.value > FocusedWeightRatio;
        WeaponData[] pool = useFallback ? fallback : preferred;

        return pool[Random.Range(0, pool.Length)];
    }
}