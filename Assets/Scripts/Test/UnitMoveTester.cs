using UnityEngine;

/// <summary>
/// 이동 로직 검증용 임시 테스트 스크립트.
/// TestUnit을 지정 좌표에 스폰하고, 키 입력으로 한 칸씩 이동시켜본다.
/// 실제 게임 로직(마우스 선택, 이동 범위 표시 등)이 만들어지면 이 스크립트는 제거해도 된다.
/// </summary>
public class UnitMoveTester : MonoBehaviour
{
    [Header("References")]
    public GridManager gridManager;
    public TestUnit testUnitPrefab; // 프리팹이 없으면 비워두고 아래에서 자동 생성됨

    [Header("Spawn Settings")]
    public Vector2Int spawnCoord = new Vector2Int(2, 2);

    private TestUnit spawnedUnit;

    void Start()
    {
        SpawnTestUnit();
    }

    void Update()
    {
        if (spawnedUnit == null) return;

        Vector2Int moveDir = Vector2Int.zero;

        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
            moveDir = Vector2Int.up;
        else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
            moveDir = Vector2Int.down;
        else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
            moveDir = Vector2Int.left;
        else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
            moveDir = Vector2Int.right;

        if (moveDir != Vector2Int.zero)
        {
            Vector2Int targetCoord = spawnedUnit.GridCoord + moveDir;
            bool success = spawnedUnit.TryMoveTo(targetCoord);

            if (!success)
                Debug.Log($"이동 실패: {targetCoord}는 이동 불가 타일이거나 범위 밖입니다.");
        }
    }

    private void SpawnTestUnit()
    {
        GameObject unitObj;

        if (testUnitPrefab != null)
        {
            unitObj = Instantiate(testUnitPrefab.gameObject);
        }
        else
        {
            // 프리팹이 없으면 코드로 즉석에서 생성 (스프라이트는 흰 사각형 + 빨간색)
            unitObj = new GameObject("TestUnit");
            unitObj.AddComponent<TestUnit>();

            SpriteRenderer sr = unitObj.AddComponent<SpriteRenderer>();
            sr.sprite = CreateSquareSprite();
            sr.color = Color.red;
            sr.sortingOrder = 1; // 타일보다 위에 그려지도록
        }

        spawnedUnit = unitObj.GetComponent<TestUnit>();
        spawnedUnit.PlaceOnGrid(spawnCoord, gridManager);
    }

    private Sprite CreateSquareSprite()
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
    }
}