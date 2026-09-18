using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 모든 유닛(캐릭터)의 기반이 되는 추상 클래스.
/// 종족 기본 스탯(Base 접두사)은 절대 직접 변경되지 않으며,
/// 장비로 인한 보정은 계산 프로퍼티(MoveRange, AttackPower 등)를 통해서만 반영된다.
///
/// 장비 슬롯 관리는 UnitEquipmentState, 상태이상 관리는 UnitStatusEffectTracker에
/// 위임되어 있다. 이 클래스는 이동/전투/행동력 관리라는 핵심 로직에 집중한다.
/// </summary>
public abstract class UnitBase : MonoBehaviour
{
    #region Events

    public event Action<UnitBase, int> OnDamaged;
    public event Action<UnitBase> OnDied;
    public event Action<UnitBase, Vector2Int, Vector2Int> OnMoved;
    public event Action<UnitBase, UnitBase> OnAttackPerformed;
    public event Action<UnitBase, UnitBase, CombatResult> OnAttackResult;
    public event Action<UnitBase> OnActionsExhausted;
    public event Action<UnitBase> OnTurnReset;

    #endregion

    #region Identity

    [Header("Grid Position")]
    [field: SerializeField] public Vector2Int GridCoord { get; private set; }

    [Header("Faction")]
    [field: SerializeField] public FactionData Faction { get; private set; }
    [field: SerializeField] public RaceData Race { get; private set; }

    #endregion

    #region Capabilities & Turn State

    // 이 유닛이 "이동"/"공격"이라는 행위 자체를 할 수 있는지 (포탑, 바리케이드 등에서 false로 고정)
    [SerializeField] private bool canMoveInnately = true;
    [SerializeField] private bool canAttackInnately = true;
    public bool CanAttack => canAttackInnately;

    [field: SerializeField] public int CurrentHealth { get; protected set; }
    [field: SerializeField] public bool HasMoved { get; private set; }
    [field: SerializeField] public int ActionsUsedThisTurn { get; private set; }

    [field: SerializeField, Tooltip("이번 턴에 방어 태세(Steady)를 취해서 막기 확률이 올라간 상태인지")]
    public bool IsBraced { get; private set; }

    [field: SerializeField, Tooltip("AI 유닛의 교전 성향. 플레이어 유닛은 null(None)")]
    public AICombatDisposition? AssignedDisposition { get; private set; }
    public IUnitAIBehavior AIBehavior { get; set; }

    public void SetAIBehavior(IUnitAIBehavior behavior, AICombatDisposition disposition)
    {
        AIBehavior = behavior;
        AssignedDisposition = disposition;
    }

    #endregion

    #region Equipment (delegated to UnitEquipmentState)

    private readonly UnitEquipmentState equipment = new UnitEquipmentState();

    public WeaponData MainHandWeapon => equipment.MainHandWeapon;
    public WeaponData OffHandWeapon => equipment.OffHandWeapon;
    public ShieldData EquippedShield => equipment.EquippedShield;
    public ArmorData EquippedArmor => equipment.EquippedArmor;
    public bool ShieldBroken => equipment.ShieldBroken;
    public bool ArmorBroken => equipment.ArmorBroken;
    public bool IsLoaded => equipment.IsLoaded;

    public bool EquipMainHandWeapon(WeaponData weapon) => equipment.EquipMainHandWeapon(weapon);
    public bool EquipOffHandWeapon(WeaponData weapon) => equipment.EquipOffHandWeapon(weapon);
    public bool EquipShield(ShieldData shield) => equipment.EquipShield(shield);
    public void EquipArmor(ArmorData armor) => equipment.EquipArmor(armor);
    public void BreakEquipment() => equipment.BreakEquipment();
    public void Reload() => equipment.Reload();
    public void ConsumeAmmo() => equipment.ConsumeAmmo();
    public int ArmorDefense => equipment.ArmorDefense;

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

    // 보조무기가 있으면 자동으로 추가공격 어빌리티를 발동시킨다
    public IEnumerable<IWeaponAbility> GetActiveAbilities()
    {
        if (OffHandWeapon != null)
            yield return new ExtraAttackAbility(OffHandWeapon);
    }

    #endregion

    #region Universal Actions (무기와 무관하게 항상 후보가 되는 행동)

    [Header("Universal Actions (방어태세 등, 무기와 무관하게 항상 선택 가능)")]
    [SerializeField] private UnitAction[] universalActions = new UnitAction[0];
    public IReadOnlyList<UnitAction> UniversalActions => universalActions;

    #endregion

    #region Status Effects (delegated to UnitStatusEffectTracker)

    private readonly UnitStatusEffectTracker statusEffects = new UnitStatusEffectTracker();

    public bool IsStunned => statusEffects.IsStunned;

    public void ApplyStatusEffect(StatusEffectType type, int duration, int magnitude) =>
        statusEffects.Apply(type, duration, magnitude);

    #endregion

    #region Calculated Stats (종족 기본치 + 장비 보정)

    public int MaxHealth => Race != null ? Race.maxHealth : 1;

    public int MoveRange => Race != null
        ? Mathf.Max(0, Race.baseMoveRange - (EquippedArmor?.moveRangePenalty ?? 0) - (EquippedShield?.moveRangePenalty ?? 0))
        : 0;

    public int MeleeSkill => Race != null ? Race.baseMeleeSkill : 0;
    public int RangedSkill => Race != null ? Race.baseRangedSkill : 0;
    public int Strength => Race != null ? Race.baseStrength : 0;
    public int Agility => Race != null ? Race.baseAgility : 0;
    public int DefenseSkill => Race != null ? Race.baseDefenseSkill : 0;

    public int ConstitutionDefense => Race != null ? Race.baseConstitution : 0;
    public int Defense => ConstitutionDefense + ArmorDefense; // 관통력 미반영 총 방어력 (UI 표시 등에 사용)

    public int BaseAttackRange = 1;
    public int AttackRange =>
        (MainHandWeapon != null && MainHandWeapon.attackRangeOverride >= 0)
            ? MainHandWeapon.attackRangeOverride
            : BaseAttackRange;

    #endregion

    #region Racial Traits

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

    // 턴당 최대 행동 횟수 (엘프의 ExtraAttackTrait 등, 기본값 1)
    public int MaxActionsPerTurn
    {
        get
        {
            int max = 1;

            if (Race?.traits != null)
            {
                foreach (var trait in Race.traits)
                    if (trait != null)
                        max = trait.ModifyMaxActions(max);
            }

            return max;
        }
    }

    // isMeleeContext가 true일 때만 근접 특성(오크의 힘 보너스 등)이 적용된 힘 반환
    public int GetEffectiveStrength(bool isMeleeContext)
    {
        int strength = Strength;

        if (isMeleeContext && Race?.traits != null)
        {
            foreach (var trait in Race.traits)
                if (trait != null)
                    strength = trait.ModifyMeleeStrength(strength);
        }

        return strength;
    }

    #endregion

    #region Action Availability

    public bool CanMove => canMoveInnately && !HasMoved;
    public bool HasActionsRemaining => ActionsUsedThisTurn < MaxActionsPerTurn;
    public bool CanStillAct => !HasMoved || HasActionsRemaining;

    #endregion

    #region Lifecycle

    protected GridManager gridManager;
    protected SpriteRenderer spriteRenderer;
    private static Sprite defaultSquareSprite;

    protected virtual void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();

        if (spriteRenderer.sprite == null)
            spriteRenderer.sprite = GetDefaultSquareSprite();

        // 타일(0), 이동범위 하이라이트(1)보다 항상 위에 그려지도록
        if (spriteRenderer.sortingOrder < 2)
            spriteRenderer.sortingOrder = 2;
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

    #endregion

    #region Faction / Race Assignment

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

    #endregion

    #region Movement

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

    public virtual bool TryMoveTo(Vector2Int targetCoord)
    {
        if (!CanMove)
            return false;

        if (gridManager == null)
        {
            Debug.LogError($"[{name}] gridManager가 설정되지 않은 채로 TryMoveTo가 호출되었습니다. PlaceOnGrid가 먼저 호출되었는지 확인하세요.");
            return false;
        }

        if (IsStunned)
        {
            Debug.Log($"[{name}] 기절 상태라 이동할 수 없습니다.");
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

    #endregion

    #region Action Point Consumption

    // 공격/재장전/방어태세 등 "행동"에 해당하는 모든 것이 이 함수를 거침
    public void ConsumeAction()
    {
        ActionsUsedThisTurn++;
        HasMoved = true; // 행동하면 이동도 함께 봉인 (기존 규칙 유지)

        if (!CanStillAct)
            OnActionsExhausted?.Invoke(this);
    }

    public void PerformReload()
    {
        if (!HasActionsRemaining)
            return;

        Reload();
        ConsumeAction();
    }

    public void EnterBracedStance()
    {
        if (!HasActionsRemaining)
            return;

        if (IsStunned)
        {
            Debug.Log($"[{name}] 기절 상태라 방어태세를 취할 수 없습니다.");
            return;
        }

        IsBraced = true;
        ConsumeAction();
    }

    #endregion

    #region Combat

    public bool IsInAttackRange(UnitBase target)
    {
        if (target == null || gridManager == null)
            return false;

        int distance = gridManager.GetDistance(GridCoord, target.GridCoord);
        return distance <= AttackRange;
    }

    public virtual bool TryAttack(UnitBase target, WeaponAttack chosenAttack = null)
    {
        if (target == null)
            return false;

        if (!CanAttack || !HasActionsRemaining)
            return false;

        if (IsStunned)
        {
            Debug.Log($"[{name}] 기절 상태라 공격할 수 없습니다.");
            return false;
        }

        // 재장전이 필요한 무기가 장전 안 된 상태면 어떤 경로로 호출되든(플레이어/AI/반격) 여기서 막는다.
        // WeaponAttack.IsAvailable()이 UI 후보에서는 이미 걸러내지만, TryAttack이 직접 호출되는
        // 다른 경로(AI 등)에서도 동일하게 보장되도록 실행 지점 자체에 둔다.
        if (MainHandWeapon != null && MainHandWeapon.requiresReload && !IsLoaded)
        {
            Debug.Log($"[{name}] 재장전이 필요해 공격할 수 없습니다.");
            return false;
        }

        if (!IsInAttackRange(target))
            return false;

        WeaponAttack attack = chosenAttack ?? MainHandWeapon?.GetDefaultAttack();

        List<CombatResult> results = CombatResolver.ResolveFullAttack(this, target, attack);
        OnAttackPerformed?.Invoke(this, target);

        foreach (var result in results)
        {
            if (target == null || target.CurrentHealth <= 0)
                break;

            OnAttackResult?.Invoke(this, target, result);

            if (result.IsBlocked)
                continue;

            if (result.IsHit)
            {
                target.TakeDamage(result.DamageDealt, this);

                if (result.InflictedEffect != StatusEffectType.None && attack != null)
                {
                    if (attack.inflictedEffect == StatusEffectType.ArmorBreak)
                        target.BreakEquipment();
                    else
                        target.ApplyStatusEffect(attack.inflictedEffect, attack.effectDuration, attack.effectMagnitude);
                }
            }
        }

        if (attack != null && MainHandWeapon != null && MainHandWeapon.requiresReload)
            ConsumeAmmo();

        ConsumeAction();

        return true;
    }

    // amount는 CombatResolver에서 이미 방어력이 반영된 최종 데미지
    // attacker가 있으면(누군가 직접 때린 경우) 반격 판정을 시도한다. 출혈 등 공격자 없는 데미지는 null.
    public virtual void TakeDamage(int amount, UnitBase attacker = null)
    {
        CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
        OnDamaged?.Invoke(this, amount);

        if (CurrentHealth <= 0)
        {
            Die();
            return;
        }


        //반격 태세 중일 시 반격
        bool canCounter = !IsStunned && MainHandWeapon != null && MainHandWeapon.grantsCounterattack;

        if (canCounter && attacker != null && IsInAttackRange(attacker))
        {
            Debug.Log($"[{name}] 반격!");
            PerformCounterattack(attacker);
        }
    }

    private void PerformCounterattack(UnitBase attacker)
    {
        // 반격도 TryAttack과 동일하게 재장전 상태를 지켜야 한다 (탄약 없는 석궁이 계속 반격하는 걸 방지)
        if (MainHandWeapon != null && MainHandWeapon.requiresReload && !IsLoaded)
        {
            Debug.Log($"[{name}] 재장전이 필요해 반격할 수 없습니다.");
            return;
        }

        WeaponAttack counterAttack = MainHandWeapon?.GetDefaultAttack();

        CombatResult result = CombatResolver.Resolve(this, attacker, MainHandWeapon, counterAttack);

        if (MainHandWeapon != null && MainHandWeapon.requiresReload)
            ConsumeAmmo(); // 반격도 발사인 건 마찬가지이므로 일반 공격과 동일하게 탄약을 소모시킨다

        if (result.IsHit && !result.IsBlocked)
            attacker.TakeDamage(result.DamageDealt); // attacker 인자 생략 -> 반격에 또 반격하지 않음
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

    #endregion

    #region Turn State

    public void ResetTurnState()
    {
        HasMoved = false;
        ActionsUsedThisTurn = 0;
        IsBraced = false;

        statusEffects.ProcessTurnStart(damage => TakeDamage(damage));

        OnTurnReset?.Invoke(this);
    }

    #endregion
}
