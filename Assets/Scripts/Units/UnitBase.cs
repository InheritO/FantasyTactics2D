using UnityEngine;
using System;

/// <summary>
/// 모든 유닛(캐릭터)의 기반이 되는 추상 클래스.
/// 그리드 좌표, 체력, 이동력 등 공통 속성과 기본 이동 로직을 담는다.
/// 실제 게임에 등장하는 유닛은 이 클래스를 상속받아 구현한다.
/// </summary>
public abstract class UnitBase : MonoBehaviour
{
    [Header("Grid Position")]
    public Vector2Int GridCoord { get; private set; }

    [Header("Faction")]
    public FactionData Faction { get; private set; }

    [Header("Stats")]
    public int MaxHealth = 10;
    public int CurrentHealth { get; protected set; }
    public int MoveRange = 3;
    public int AttackRange = 1;
    public int AttackPower = 3;

    public event Action<UnitBase, int> OnDamaged;
    public event Action<UnitBase> OnDied;


    public bool HasActedThisTurn { get; private set; } = false;

    public IUnitAIBehavior AIBehavior { get; set; }

    public bool CanMove { get; protected set; } = true;
    public bool CanAttack { get; protected set; } = true;

    protected GridManager gridManager;
    protected SpriteRenderer spriteRenderer;

    protected virtual void Awake()
    {
        CurrentHealth = MaxHealth;
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer == null)
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();

        if (spriteRenderer.sprite == null)
            spriteRenderer.sprite = GetDefaultSquareSprite();
    }

    // 세력 지정 (스폰 시 호출)
    public virtual void SetFaction(FactionData faction)
    {
        Faction = faction;

        if (spriteRenderer != null && faction != null)
            spriteRenderer.color = faction.factionColor;
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
        if (!CanMove)
            return false;

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

        target.TakeDamage(AttackPower);
        return true;
    }
    public virtual void TakeDamage(int amount)
    {
        CurrentHealth = Mathf.Max(0, CurrentHealth - amount);

        if (CurrentHealth <= 0)
            Die();
    }

    protected virtual void Die()
    {
        TileInstance tile = gridManager.GetTile(GridCoord);
        if (tile != null && tile.OccupyingUnit == this)
            tile.OccupyingUnit = null;

        Destroy(gameObject);
    }

    // 자식 클래스가 반드시 구현해야 하는, 유닛 고유의 행동 (공격, 스킬 등)
    public abstract void PerformAction();

    private static Sprite defaultSquareSprite;

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

    public void ResetTurnState()
    {
        HasActedThisTurn = false;
    }

    public void MarkAsActed()
    {
        HasActedThisTurn = true;
    }
}