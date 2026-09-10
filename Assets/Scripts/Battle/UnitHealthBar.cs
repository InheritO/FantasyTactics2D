using UnityEngine;

/// <summary>
/// 유닛 머리 위에 표시되는 체력바. 배경 + 채움 두 개의 스프라이트로 구성되며,
/// OnDamaged 이벤트를 구독해 자동으로 갱신된다. 유닛의 자식으로 붙어 위치를 자동으로 따라간다.
/// </summary>
public class UnitHealthBar : MonoBehaviour
{
    private UnitBase unit;
    private SpriteRenderer fillRenderer;

    private const float BarWidth = 0.8f;
    private const float BarHeight = 0.1f;
    private static readonly Vector3 Offset = new Vector3(0f, 0.6f, 0f);

    private static Sprite cachedSprite;

    public void Initialize(UnitBase targetUnit)
    {
        unit = targetUnit;

        transform.SetParent(unit.transform, false);
        transform.localPosition = Offset;

        CreateBackground();
        fillRenderer = CreateFill();

        unit.OnDamaged += HandleDamaged;

        UpdateBar();
    }

    private void CreateBackground()
    {
        GameObject bg = new GameObject("Background");
        bg.transform.SetParent(transform, false);

        SpriteRenderer sr = bg.AddComponent<SpriteRenderer>();
        sr.sprite = GetSquareSprite();
        sr.color = Color.black;
        sr.sortingOrder = 3; // 유닛(2)보다 위
        bg.transform.localScale = new Vector3(BarWidth + 0.04f, BarHeight + 0.04f, 1f);
    }

    private SpriteRenderer CreateFill()
    {
        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(transform, false);

        SpriteRenderer sr = fill.AddComponent<SpriteRenderer>();
        sr.sprite = GetSquareSprite();
        sr.sortingOrder = 4; // 배경보다 위
        fill.transform.localScale = new Vector3(BarWidth, BarHeight, 1f);

        return sr;
    }

    private void HandleDamaged(UnitBase u, int amount) => UpdateBar();

    private void UpdateBar()
    {
        if (unit == null || fillRenderer == null)
            return;

        float ratio = unit.MaxHealth > 0 ? (float)unit.CurrentHealth / unit.MaxHealth : 0f;
        ratio = Mathf.Clamp01(ratio);

        fillRenderer.transform.localScale = new Vector3(BarWidth * ratio, BarHeight, 1f);

        // pivot이 중앙이라, 줄어든 만큼 왼쪽으로 당겨줘야 왼쪽 기준으로 닳는 것처럼 보임
        float offsetX = -(BarWidth - BarWidth * ratio) / 2f;
        fillRenderer.transform.localPosition = new Vector3(offsetX, 0f, 0f);

        fillRenderer.color = Color.Lerp(Color.red, Color.green, ratio);
    }

    private Sprite GetSquareSprite()
    {
        if (cachedSprite == null)
        {
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            cachedSprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        }
        return cachedSprite;
    }

    void OnDestroy()
    {
        if (unit != null)
            unit.OnDamaged -= HandleDamaged;
    }
}