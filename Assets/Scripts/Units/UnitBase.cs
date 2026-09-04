using UnityEngine;
using System;
using System.Collections.Generic;
using NaughtyAttributes;

/// <summary>
/// 모든 유닛(캐릭터)의 기반이 되는 추상 클래스.
/// 종족 기본 스탯(Base 접두사)은 절대 직접 변경되지 않으며,
/// 장비로 인한 보정은 계산 프로퍼티(MoveRange, AttackPower 등)를 통해서만 반영된다.
/// </summary>
public abstract class UnitBase : MonoBehaviour
{
    public event Action<UnitBase, int> OnDamaged;
    public event Action<UnitBase> OnDied;

    [Header("Grid Position")]
    public Vector2Int GridCoord { get; private set; }

    [Header("Faction")]
    public FactionData Faction { get; private set; }
    public RaceData Race { get; private set; }



    [Header("Capabilities")]
    public bool CanMove { get; protected set; } = true;
    public bool CanAttack { get; protected set; } = true;

    [field:SerializeField]
    public int CurrentHealth { get; protected set; }
    public bool HasActedThisTurn { get; private set; } = false;
    public IUnitAIBehavior AIBehavior { get; set; }

    /// <summary>
    /// 장비 슬롯
    /// </summary>
    [field: SerializeField]
    public WeaponData MainHandWeapon { get; private set; }
    public WeaponData OffHandWeapon { get; private set; } // 두 번째 한손무기일 수도 있음
    public ShieldData EquippedShield { get; private set; }

    [field: SerializeField]
    public ArmorData EquippedArmor { get; private set; }

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
    public int Strength => Race != null ? Race.baseStrength : 0;
    public int Agility => Race != null ? Race.baseAgility : 0;


    // Defense를 두 요소로 분리: 관통력이 ArmorDefense에만 영향을 주기 위함
    public int ConstitutionDefense => Race != null ? Race.baseConstitution : 0;
    public int ArmorDefense => (EquippedArmor?.defenseBonus ?? 0) + (EquippedShield?.defenseBonus ?? 0);
    public int Defense => ConstitutionDefense + ArmorDefense; // 관통력 미반영 총 방어력 (UI 표시 등에 사용)

    public int BaseAttackRange = 1;
    public int AttackRange =>
        (MainHandWeapon != null && MainHandWeapon.attackRangeOverride >= 0)
            ? MainHandWeapon.attackRangeOverride
            : BaseAttackRange;

    protected virtual void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();

        if (spriteRenderer.sprite == null)
            spriteRenderer.sprite = GetDefaultSquareSprite();
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
        Faction = faction;
        Race = faction.race;

        CurrentHealth = MaxHealth; // Race가 확정된 시점에 체력 초기화

        if (spriteRenderer != null && faction != null)
            spriteRenderer.color = faction.factionColor;
    }

    //장비

    public bool EquipMainHandWeapon(WeaponData weapon)
    {
        if (weapon == null)
        {
            MainHandWeapon = null;
            return true;
        }

        MainHandWeapon = weapon;

        if (weapon.handedness == WeaponHandedness.TwoHanded)
        {
            // 양손 무기는 보조 슬롯을 전부 비움
            OffHandWeapon = null;
            EquippedShield = null;
        }

        return true;
    }

    public bool EquipOffHandWeapon(WeaponData weapon)
    {
        if (weapon != null && weapon.handedness == WeaponHandedness.TwoHanded)
        {
            Debug.Log("양손 무기는 보조 슬롯에 장착할 수 없습니다.");
            return false;
        }

        if (MainHandWeapon != null && MainHandWeapon.handedness == WeaponHandedness.TwoHanded)
        {
            Debug.Log("양손 무기를 장착 중이라 보조 무기를 장착할 수 없습니다.");
            return false;
        }

        OffHandWeapon = weapon;

        if (weapon != null)
            EquippedShield = null; // 보조무기와 방패는 같은 슬롯을 두고 경쟁

        return true;
    }

    public bool EquipShield(ShieldData shield)
    {
        if (MainHandWeapon != null && MainHandWeapon.handedness == WeaponHandedness.TwoHanded)
        {
            Debug.Log("양손 무기를 장착 중이라 방패를 장착할 수 없습니다.");
            return false;
        }

        EquippedShield = shield;

        if (shield != null)
            OffHandWeapon = null;

        return true;
    }

    public void EquipArmor(ArmorData armor) => EquippedArmor = armor;

    // 무기 어빌리티
    public IEnumerable<IWeaponAbility> GetActiveAbilities()
    {
        if (OffHandWeapon != null)
            yield return new ExtraAttackAbility(OffHandWeapon);
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
        if (!CanMove)
            return false;

        TileInstance targetTile = gridManager.GetTile(targetCoord);
        if (targetTile == null || !targetTile.IsWalkable())
            return false;

        TileInstance currentTile = gridManager.GetTile(GridCoord);
        if (currentTile != null)
            currentTile.OccupyingUnit = null;

        GridCoord = targetCoord;
        transform.position = gridManager.GridToWorld(targetCoord);
        targetTile.OccupyingUnit = this;

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
    public virtual bool TryAttack(UnitBase target)
    {
        if (!CanAttack)
            return false;

        if (!IsInAttackRange(target))
            return false;

        List<CombatResult> results = CombatResolver.ResolveFullAttack(this, target);

        foreach (var result in results)
        {
            if (target == null || target.CurrentHealth <= 0)
                break;

            if (result.IsHit)
                target.TakeDamage(result.DamageDealt);
            else
                Debug.Log($"{name}의 공격이 빗나갔습니다.");
        }

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
        TileInstance tile = gridManager.GetTile(GridCoord);
        if (tile != null && tile.OccupyingUnit == this)
            tile.OccupyingUnit = null;

        OnDied?.Invoke(this);

        Destroy(gameObject);
    }



    // ---- 턴 상태 ----

    public void ResetTurnState()
    {
        HasActedThisTurn = false;
    }

    public void MarkAsActed()
    {
        HasActedThisTurn = true;
    }


}