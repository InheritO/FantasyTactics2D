using UnityEngine;

public enum VisualLayerSource { Body, Head, Armor, MainHandWeapon, OffHandSlot }

[System.Serializable]
public class VisualLayer
{
    public VisualLayerSource source;
    public SpriteRenderer renderer;

    [System.NonSerialized] public CharacterAnimationSet set;
}

public class UnitVisualController : MonoBehaviour
{
    public VisualLayer[] layers;
    public float frameRate = 6f;

    private UnitBase unit;
    private int frameIndex;
    private float frameTimer;

    public Transform visualRoot;

    void Awake() => unit = GetComponent<UnitBase>();

    // 스폰 시점(Race/장비 확정 직후)에 UnitSpawner가 호출
    public void RefreshVisuals()
    {
        foreach (var layer in layers)
            layer.set = ResolveSet(layer.source);


        if (visualRoot != null && unit.Race != null)
            visualRoot.localScale = Vector3.one * unit.Race.visualScale;
    }

    private CharacterAnimationSet ResolveSet(VisualLayerSource source)
    {
        switch (source)
        {
            case VisualLayerSource.Body: return unit.Race != null ? unit.Race.visualSet : null;
            case VisualLayerSource.Armor: return unit.EquippedArmor != null ? unit.EquippedArmor.visualSet : null;
            case VisualLayerSource.MainHandWeapon: return unit.MainHandWeapon != null ? unit.MainHandWeapon.visualSet : null;
            case VisualLayerSource.OffHandSlot:
                if (unit.OffHandWeapon != null) return unit.OffHandWeapon.visualSet;
                if (unit.EquippedShield != null) return unit.EquippedShield.visualSet;
                return null;
            case VisualLayerSource.Head:
                if (unit.EquippedArmor != null && unit.EquippedArmor.headVisualSet != null)
                    return unit.EquippedArmor.headVisualSet; // 투구 있는 갑옷이면 우선
                return unit.Race != null ? unit.Race.headVisualSet : null; // 없으면 종족 기본 머리
            default: return null;
        }
    }

    // 지금은 대기(idle)만 재생 — 공격 애니메이션 트리거는 다음 단계에서 추가
    private CharacterAnimationSet.DirectionalFrames CurrentAnimation(CharacterAnimationSet set)
    {
        return set != null ? set.idle : null;
    }

    void Update()
    {
        if (layers == null || layers.Length == 0)
            return;

        frameTimer += Time.deltaTime;
        if (frameTimer >= 1f / frameRate)
        {
            frameTimer = 0f;
            frameIndex++;
        }

        foreach (var layer in layers)
        {
            if (layer.renderer == null)
                continue;

            Sprite[] frames = CurrentAnimation(layer.set)?.Get(unit.FacingDirection);

            if (frames == null || frames.Length == 0)
            {
                layer.renderer.enabled = false; // 장비 없음(방패 미착용 등) -> 안 보이게
                continue;
            }

            layer.renderer.enabled = true;
            layer.renderer.sprite = frames[frameIndex % frames.Length];
        }
    }

    public SpriteRenderer GetRenderer(VisualLayerSource source)
    {
        foreach (var layer in layers)
            if (layer.source == source)
                return layer.renderer;

        return null;
    }
}