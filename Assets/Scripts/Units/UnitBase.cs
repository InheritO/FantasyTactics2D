using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// 모든 유닛(캐릭터)의 기반이 되는 추상 클래스.
/// 종족 기본 스탯(Base 접두사)은 절대 직접 변경되지 않으며,
/// 장비로 인한 보정은 계산 프로퍼티(MoveRange, AttackPower 등)를 통해서만 반영된다.
/// </summary>
public abstract class UnitBase : MonoBehaviour
{

    [Header("Grid Position")]
    [field: SerializeField] public Vector2Int GridCoord { get; private set; }

    [Header("Faction")]
    [field: SerializeField] public FactionData Faction { get; private set; }
    [field: SerializeField] public RaceData Race { get; private set; }



    [Header("Capabilities")]
    [field: SerializeField] public bool CanMove { get; protected set; } = true;
    [field: SerializeField] public bool CanAttack { get; protected set; } = true;

    [field: SerializeField] public int CurrentHealth { get; protected set; }
    public IUnitAIBehavior AIBehavior { get; set; }
    [Header("AI 확인용")]
    public AICombatDisposition AssignedDisposition;


    /// <summary>
    /// 장비 슬롯
    /// </summary>
    [field: SerializeField] public WeaponData MainHandWeapon { get; private set; }
    [field: SerializeField] public WeaponData OffHandWeapon { get; private set; } // 두 번째 한손무기일 수도 있음
    [field: SerializeField] public ShieldData EquippedShield { get; private set; }

    [field: SerializeField] public ArmorData EquippedArmor { get; private set; }

    protected GridManager gridManager;
    protected SpriteRenderer spriteRenderer;
    private static Sprite defaultSquareSprite;

    // ---- 계산 스탯 (종족 기본치 + 장비 보정) ----

    public int MaxHealth => Race != null ? Race.maxHealth : 1;
    public int MoveRange => Race != null
        ? Mathf.Max(0, Race.baseMoveRange - (EquippedArmor?.moveRangePenalty ?? 0))
        : 0;
    public int MeleeSkill => Race != null ? Race.baseMeleeSkill : 0;
    public int RangedSkill => Race != null ? Race.baseRangedSkill : 0;
    public int DefenseSkill => Race != null ? Race.baseDefenseSkill : 0;
    public int Strength => Race != null ? Race.baseStrength : 0;
    public int Agility => Race != null ? Race.baseAgility : 0;


    // Defense를 두 요소로 분리: 관통력이 ArmorDefense에만 영향을 주기 위함
    public int ConstitutionDefense => Race != null ? Race.baseConstitution : 0;
    public int ArmorDefense =>
    (ArmorBroken ? 0 : (EquippedArmor?.defenseBonus ?? 0)) +
    (ShieldBroken ? 0 : (EquippedShield?.defenseBonus ?? 0));
    public int Defense => ConstitutionDefense + ArmorDefense; // 관통력 미반영 총 방어력 (UI 표시 등에 사용)

    public int BaseAttackRange = 1;
    public int AttackRange =>
        (MainHandWeapon != null && MainHandWeapon.attackRangeOverride >= 0)
            ? MainHandWeapon.attackRangeOverride
            : BaseAttackRange;

    // 행동 관련
    public bool HasMoved { get; private set; }
    public bool HasAttacked { get; private set; }

    // ---- 공격 횟수 전환: HasAttacked(bool) -> AttacksUsedThisTurn(int) ----
    public int AttacksUsedThisTurn { get; private set; }


    // 지금 이 유닛이 뭔가 더 할 수 있는지 (이동 or 공격 중 하나라도 안 했으면 true)

    public bool CanStillAct => !HasMoved || AttacksUsedThisTurn < MaxAttacksPerTurn;

    // 상태이상
    private List<StatusEffectInstance> activeEffects = new List<StatusEffectInstance>();
    // 기절 상태인지 여부 (이동/공격 가능 여부 판정에 사용)
    public bool IsStunned => activeEffects.Exists(e => e.Type == StatusEffectType.Stun);
    public bool ShieldBroken { get; private set; }
    public bool ArmorBroken { get; private set; }


    //이벤트

    public event Action<UnitBase, int> OnDamaged;
    public event Action<UnitBase> OnDied;
    public event Action<UnitBase, Vector2Int, Vector2Int> OnMoved;
    public event Action<UnitBase, UnitBase> OnAttackPerformed;
    public event Action<UnitBase, UnitBase, CombatResult> OnAttackResult;
    public event Action<UnitBase> OnActionsExhausted;
    public event Action<UnitBase> OnTurnReset;

    public event Action<UnitBase> OnEquipmentChanged;
    private int unitSortOrder = 2;

    protected virtual void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();

        if (spriteRenderer.sprite == null)
            spriteRenderer.sprite = GetDefaultSquareSprite();

        if (spriteRenderer.sortingOrder < unitSortOrder)
            spriteRenderer.sortingOrder = unitSortOrder;
    }

    private static Sprite GetDefaultSquareSprite()
    {
        if (defaultSquareSprite == null)
        {
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            defaultSquareSprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        }
        return defaultSquareSprite;
    }


    // 세력 지정 (스폰 시 호출)
    public virtual void SetFaction(FactionData faction)
    {
        if (faction == null)
        {
            Debug.LogError($"[{name}] null인 FactionData로 SetFaction이 호출되었습니다.");
            return;
        }

        Faction = faction;
        Race = faction.race;

        if (Race == null)
            Debug.LogWarning($"[{faction.factionName}] 세력에 Race가 설정되지 않았습니다. 스탯이 기본값(0)으로 처리됩니다.");

        CurrentHealth = MaxHealth; // Race가 확정된 시점에 체력 초기화

        if (spriteRenderer != null)
            spriteRenderer.color = faction.factionColor;
    }

    // 대전 모드에서 사용: 이 유닛의 종족을 명시적으로 지정한다.
    // (캠페인 모드는 SetFaction()이 FactionData.race를 그대로 따르는 기존 방식을 유지한다.)
    public void AssignRace(RaceData race)
    {
        if (race == null)
        {
            Debug.LogWarning($"[{name}] AssignRace에 null이 전달되었습니다.");
            return;
        }

        Race = race;
        CurrentHealth = MaxHealth; // 종족이 바뀌면 최대체력도 바뀌므로 재초기화
    }

    // 세력의 기본 종족과 무관하게, 스폰 시점에 실제 종족을 명시적으로 지정
    public void OverrideRace(RaceData race)
    {
        if (race == null)
        {
            Debug.LogWarning($"[{name}] OverrideRace에 null이 전달되었습니다.");
            return;
        }

        Race = race;
        CurrentHealth = MaxHealth;
    }

    //장비

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

        if (weapon.handedness == WeaponHandedness.TwoHanded)
        {
            // 양손 무기는 보조 슬롯을 전부 비움
            OffHandWeapon = null;
            EquippedShield = null;
        }

        OnEquipmentChanged?.Invoke(this);
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
        EquippedShield = null; // 방패와 보조무기는 함께 착용 불가함

        OnEquipmentChanged?.Invoke(this);
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

        OnEquipmentChanged?.Invoke(this);
        return true;
    }

    public void EquipArmor(ArmorData armor) => EquippedArmor = armor;

    // 무기 어빌리티
    public IEnumerable<IWeaponAbility> GetActiveAbilities()
    {
        if (OffHandWeapon != null)
            yield return new ExtraAttackAbility(OffHandWeapon);
    }

    // RosterEntry의 장비 구성을 그대로 적용
    public void ApplyLoadout(RosterEntry entry)
    {
        if (entry == null)
            return;

        EquipMainHandWeapon(entry.mainHandWeapon);
        EquipOffHandWeapon(entry.offHandWeapon);
        EquipShield(entry.shield);
        EquipArmor(entry.armor);
    }

    // 유닛을 특정 그리드 좌표에 배치 (최초 배치, 순간이동 등에 사용)
    public virtual void PlaceOnGrid(Vector2Int coord, GridManager grid)
    {
        gridManager = grid;

        TileInstance previousTile = gridManager.GetTile(GridCoord);
        if (previousTile != null && previousTile.OccupyingUnit == this)
            previousTile.OccupyingUnit = null;

        GridCoord = coord;
        transform.position = gridManager.GridToWorld(coord);

        TileInstance newTile = gridManager.GetTile(coord);
        if (newTile != null)
            newTile.OccupyingUnit = this;
    }


    // 인접한 한 칸으로 이동 시도 (이동 가능하면 true 반환)
    public virtual bool TryMoveTo(Vector2Int targetCoord)
    {
        if (!CanMove || HasMoved)
            return false;

        if (IsStunned)
        {
            Debug.Log($"[{name}] 기절 상태라 이동할 수 없습니다.");
            return false;
        }


        if (gridManager == null)
        {
            Debug.LogError($"[{name}] gridManager가 설정되지 않은 채로 TryMoveTo가 호출되었습니다. PlaceOnGrid가 먼저 호출되었는지 확인하세요.");
            return false;
        }

        TileInstance targetTile = gridManager.GetTile(targetCoord);
        if (targetTile == null || !targetTile.IsWalkable())
            return false;

        TileInstance currentTile = gridManager.GetTile(GridCoord);
        if (currentTile != null)
            currentTile.OccupyingUnit = null;

        Vector2Int previousCoord = GridCoord;
        GridCoord = targetCoord;
        transform.position = gridManager.GridToWorld(targetCoord);
        targetTile.OccupyingUnit = this;

        HasMoved = true;
        OnMoved?.Invoke(this, previousCoord, targetCoord);

        if (!CanStillAct)
            OnActionsExhausted?.Invoke(this);

        return true;
    }

    // 대상이 공격 사거리 안에 있는지 확인
    public bool IsInAttackRange(UnitBase target)
    {
        if (target == null || gridManager == null)
            return false;

        int distance = gridManager.GetDistance(GridCoord, target.GridCoord);
        return distance <= AttackRange;
    }


    // 대상을 공격 시도 (사거리 밖이면 실패)
    public virtual bool TryAttack(UnitBase target, WeaponAttack chosenAttack = null)
    {
        if (target == null)
            return false;

        if (!CanAttack || AttacksUsedThisTurn >= MaxAttacksPerTurn)
            return false;


        if (IsStunned)
        {
            Debug.Log($"[{name}] 기절 상태라 공격할 수 없습니다.");
            return false;
        }

        if (!IsInAttackRange(target))
            return false;

        WeaponAttack attack = chosenAttack ?? MainHandWeapon?.GetDefaultAttack();

        List<CombatResult> results = CombatResolver.ResolveFullAttack(this, target, chosenAttack ?? MainHandWeapon?.GetDefaultAttack());
        OnAttackPerformed?.Invoke(this, target);

        foreach (var result in results)
        {
            if (target == null || target.CurrentHealth <= 0)
                break;

            OnAttackResult?.Invoke(this, target, result);

            if (result.IsHit)
            {
                target.TakeDamage(result.DamageDealt);

                // 상태이상이 발동했다면 대상에게 적용
                if (result.InflictedEffect != StatusEffectType.None)
                {
                    WeaponAttack attackUsed = chosenAttack ?? MainHandWeapon?.GetDefaultAttack();
                    if (attackUsed != null)
                    {
                        if (attackUsed.inflictedEffect == StatusEffectType.ArmorBreak)
                            target.BreakEquipment();
                        else
                            target.ApplyStatusEffect(attackUsed.inflictedEffect, attackUsed.effectDuration, attackUsed.effectMagnitude);

                    }
                }
            }
        }

        AttacksUsedThisTurn++;
        HasMoved = true; // 기존 규칙 유지: 공격하면 이동도 함께 봉인

        // 남은 공격 횟수를 다 썼을 때만 "행동 종료" 이벤트 발동 (엘프처럼 여러 번 공격 가능하면 아직 안 끝났을 수 있음)
        if (AttacksUsedThisTurn >= MaxAttacksPerTurn)
            OnActionsExhausted?.Invoke(this);

        return true;
    }

    // amount는 CombatResolver에서 이미 방어력이 반영된 최종 데미지
    public virtual void TakeDamage(int amount)
    {
        CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
        OnDamaged?.Invoke(this, amount);

        if (CurrentHealth <= 0)
            Die();
    }


    protected virtual void Die()
    {
        if (gridManager != null)
        {
            TileInstance tile = gridManager.GetTile(GridCoord);
            if (tile != null && tile.OccupyingUnit == this)
                tile.OccupyingUnit = null;
        }
        else
        {
            Debug.LogWarning($"[{name}] gridManager가 설정되지 않은 채로 사망 처리되었습니다.");
        }

        OnDied?.Invoke(this);

        Destroy(gameObject);
    }

    // 상태이상
    public void ApplyStatusEffect(StatusEffectType type, int duration, int magnitude)
    {
        StatusEffectInstance existing = activeEffects.Find(e => e.Type == type);

        if (existing != null)
        {
            // 지속시간: 항상 새로 거는 값으로 갱신 (계속 공격하면 "덧입혀서" 유지되는 느낌)
            // 데미지: 더 강한 공격으로 걸었다면 그 값으로, 약한 공격이면 기존 값 유지
            int strongerMagnitude = Mathf.Max(existing.Magnitude, magnitude);
            activeEffects.Remove(existing);
            activeEffects.Add(new StatusEffectInstance(type, duration, strongerMagnitude));
        }
        else
        {
            activeEffects.Add(new StatusEffectInstance(type, duration, magnitude));
        }
    }

    // 턴 시작 시 호출: 출혈 데미지 적용, 지속시간 감소, 만료된 효과 제거
    public void ProcessStatusEffectsOnTurnStart()
    {
        foreach (var effect in activeEffects)
        {
            if (effect.Type == StatusEffectType.Bleed)
            {
                TakeDamage(effect.Magnitude);
                Debug.Log($"[{name}] 출혈로 {effect.Magnitude} 데미지");
            }

            effect.DecrementTurn();
        }

        activeEffects.RemoveAll(e => e.IsExpired);
    }

    public void BreakEquipment()
    {
        if (EquippedShield != null && !ShieldBroken)
        {
            ShieldBroken = true;
            Debug.Log($"[{name}]의 방패가 파괴되었습니다!");
            return;
        }

        if (!ArmorBroken)
        {
            ArmorBroken = true;
            Debug.Log($"[{name}]의 방어구가 파괴되었습니다!");
        }
    }



    // 특성 계산

    // 이동 비용 계산 시 특성 반영 (드워프의 지형 할인 등)
    public int GetEffectiveMoveCost(TileInstance tile)
    {
        int cost = tile.GetMovementCost();

        if (Race?.traits != null)
        {
            foreach (var trait in Race.traits)
                if (trait != null)
                    cost = trait.ModifyMoveCost(tile, cost);
        }

        return cost;
    }

    // 턴당 최대 공격 횟수 (엘프의 추가 공격 등, 기본값 1)
    public int MaxAttacksPerTurn
    {
        get
        {
            int max = 1;

            if (Race?.traits != null)
            {
                foreach (var trait in Race.traits)
                    if (trait != null)
                        max = trait.ModifyMaxAttacks(max);
            }

            return max;
        }
    }

    // isMeleeContext가 true일 때만 근접 특성(오크의 힘 보너스 등)이 적용된 힘 반환
    public int GetEffectiveStrength(bool isMeleeContext)
    {
        int strength = Strength; // 종족 기본 힘 (계산 프로퍼티, 기존 그대로)

        if (isMeleeContext && Race?.traits != null)
        {
            foreach (var trait in Race.traits)
                if (trait != null)
                    strength = trait.ModifyMeleeStrength(strength);
        }

        return strength;
    }



    // ---- 턴 상태 ----

    public void ResetTurnState()
    {
        HasMoved = false;
        AttacksUsedThisTurn = 0;

        ProcessStatusEffectsOnTurnStart();

        OnTurnReset?.Invoke(this);
    }

    public void SetAIBehavior(IUnitAIBehavior behavior, AICombatDisposition disposition)
    {
        AIBehavior = behavior;
        AssignedDisposition = disposition;
    }
}