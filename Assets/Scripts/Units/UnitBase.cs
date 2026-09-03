using UnityEngine;
using System;
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

    [field: SerializeField]
    public WeaponData EquippedWeapon { get; private set; }
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
    public int Defense => (Race != null ? Race.baseConstitution : 0) + (EquippedArmor?.defenseBonus ?? 0);

    public int BaseAttackRange = 1; // 종족이 아닌 유닛 개체 특성으로 남겨둠 (필요시 이것도 Race로 옮길 수 있음)
    public int AttackRange =>
        (EquippedWeapon != null && EquippedWeapon.attackRangeOverride >= 0)
            ? EquippedWeapon.attackRangeOverride
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
    public void EquipWeapon(WeaponData weapon) => EquippedWeapon = weapon;
    public void EquipArmor(ArmorData armor) => EquippedArmor = armor;

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

        CombatResult result = CombatResolver.Resolve(this, target);

        if (result.IsHit)
            target.TakeDamage(result.DamageDealt);
        else
            Debug.Log($"{name}의 공격이 빗나갔습니다.");

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