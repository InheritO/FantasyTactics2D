using UnityEngine;

/// <summary>
/// 디자인 에셋 없이 단색 사각형 스프라이트를 코드로 만들어 재사용하기 위한 유틸리티.
/// UnitHealthBar가 쓰던 방식을 다른 곳(FactionMarker 등)에서도 쓸 수 있게 공용으로 뺐다.
/// </summary>
public static class ProceduralSpriteUtility
{
    private static Sprite cachedSquareSprite;

    public static Sprite GetSquareSprite()
    {
        if (cachedSquareSprite == null)
        {
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            cachedSquareSprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        }
        return cachedSquareSprite;
    }
}