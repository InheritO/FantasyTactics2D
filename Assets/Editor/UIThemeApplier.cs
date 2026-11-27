using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 시안(Main / Guide / Options / Roster)의 색과 스프라이트를 기존 UI에 입히는 에디터 전용 도구.
/// 레이아웃과 런타임 스크립트는 건드리지 않고, Image/TMP 속성 변경과 장식(Deco_) 오브젝트 추가만 한다.
///
/// 메뉴
///   Tools/UI Theme/1. Apply to Prefabs   : Assets/Prefabs/UI 프리팹 저장 (Undo 불가 → 실행 전 커밋)
///   Tools/UI Theme/2. Apply to Open Scene : 열린 PlayScene 수정 (Undo 가능, 저장은 직접)
///
/// 두 메뉴 모두 여러 번 실행해도 결과가 같다 (Deco_ 오브젝트는 이름으로 찾아 재사용).
/// </summary>
public static class UIThemeApplier
{
    // ─────────────────────────────────────────────────────────────
    // 테마 설정 (여기만 고치면 전체에 반영)
    // ─────────────────────────────────────────────────────────────

    private static readonly Color Gold       = Rgb(224, 184, 114);
    private static readonly Color Leather    = Rgb(120,  78,  46);
    private static readonly Color Red        = Rgb(150,  52,  40);
    private static readonly Color Parchment  = Rgb(242, 226, 194);
    private static readonly Color Dark       = Rgb( 58,  44,  34);
    private static readonly Color CreamText  = Rgb(242, 226, 194);
    private static readonly Color BrownText  = Rgb( 61,  46,  28);
    private static readonly Color MenuBackground = new Color32(23, 18, 14, 240);

    // Bars_Unity 슬라이스
    private const string FillRound       = "Bars_Unity_7";   // 둥근 판 (9-slice 30)
    private const string FrameRound      = "Bars_Unity_5";   // 둥근 테두리
    private const string FillPill        = "Bars_Unity_18";  // 알약형 판 (9-slice 46)
    private const string FramePill       = "Bars_Unity_16";  // 알약형 테두리
    private const string FramedParchment = "Bars_Unity_20";  // 테두리 양피지
    private const string GemSprite       = "Bars_Unity_19";  // 보석 (슬라이더 손잡이)
    private const string CornerSprite    = "Bars_Unity_33";  // L자 모서리 장식 (왼쪽 아래 기준)
    private const string DividerSprite   = "Bars_Unity_38";  // 가로 구분선

    // 9-slice 모서리를 얼마나 작게 그릴지 (값이 클수록 모서리가 작아짐)
    private const float ButtonSliceScale   = 4f;
    private const float PopupSliceScale    = 2f;

    // 강조(붉은 판)로 칠할 씬 버튼 이름
    private static readonly string[] PrimaryButtons = { "UIButton_Play", "UIButton_Confirm", "UIButton_AddUnit" };

    private const string GameTitle    = "Fantasy Tactics";
    private const string GameSubtitle = "타일 기반 턴제 전략";

    // 경로
    private const string BarsPath    = "Assets/ThirdParty/ImageResources/Free UI build package/Art/Bars_Unity.png";
    private const string FontPath    = "Assets/ThirdParty/Fonts/MaruBuri-Regular SDF.asset";
    private const string PrefabDir   = "Assets/Prefabs/UI";
    private const string PortraitDir = "Assets/Art/UI/Portraits";
    private static readonly string[] PortraitRaces = { "human", "elf", "dwarf", "orc" };

    private const string DecoPrefix = "Deco_";

    // ─────────────────────────────────────────────────────────────
    // 메뉴
    // ─────────────────────────────────────────────────────────────

    [MenuItem("Tools/UI Theme/1. Apply to Prefabs")]
    private static void ApplyPrefabsMenu()
    {
        if (!EditorUtility.DisplayDialog("UI Theme",
                "Assets/Prefabs/UI 의 프리팹을 덮어씁니다.\n프리팹 저장은 Undo 로 되돌릴 수 없으니, 먼저 커밋해 두세요.",
                "적용", "취소"))
            return;

        if (!LoadSprites()) return;

        EditPrefab("UIButton", StyleButtonPrefab);
        EditPrefab("UIPopup", StylePopupPrefab);
        EditPrefab("UIPopupScroll", StylePopupPrefab);
        EditPrefab("UIDropDown", StyleDropdownPrefab);
        EditPrefab("UIRemovableListItem", StyleListItemPrefab);

        AssetDatabase.SaveAssets();
        Debug.Log("[UI Theme] 프리팹 적용 완료");
    }

    [MenuItem("Tools/UI Theme/2. Apply to Open Scene")]
    private static void ApplySceneMenu()
    {
        if (!LoadSprites()) return;

        Scene scene = SceneManager.GetActiveScene();
        Canvas canvas = scene.GetRootGameObjects()
            .Select(go => go.GetComponent<Canvas>())
            .FirstOrDefault(c => c != null);
        if (canvas == null)
        {
            Debug.LogError("[UI Theme] 열린 씬의 루트에서 Canvas 를 찾지 못했습니다. PlayScene 을 열고 실행하세요.");
            return;
        }

        EnsurePortraitImport();

        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName(UndoName);
        int group = Undo.GetCurrentGroup();
        sceneMode = true;

        Transform root = canvas.transform;
        StyleScenePopups(root);
        StylePrimaryButtons(root);
        StyleMainMenu(root.Find("MainMenuPanel"));
        StyleRoster(root.Find("RosterPanel"));

        sceneMode = false;
        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log("[UI Theme] 씬 적용 완료 (저장은 Ctrl+S 로 직접, 되돌리기는 Ctrl+Z)");
    }

    // ─────────────────────────────────────────────────────────────
    // 프리팹
    // ─────────────────────────────────────────────────────────────

    private static void EditPrefab(string name, System.Action<GameObject> style)
    {
        string path = $"{PrefabDir}/{name}.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
        {
            Debug.LogWarning($"[UI Theme] 프리팹 없음: {path}");
            return;
        }

        GameObject contents = PrefabUtility.LoadPrefabContents(path);
        try
        {
            style(contents);
            PrefabUtility.SaveAsPrefabAsset(contents, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(contents);
        }
    }

    private static void StyleButtonPrefab(GameObject root)
    {
        SetImage(root.GetComponent<Image>(), FillRound, Leather, ButtonSliceScale);
        SetImage(FindImage(root.transform, "Trim"), FrameRound, Gold, ButtonSliceScale);
        SetText(root.transform.Find("Text (TMP)"), CreamText);
    }

    private static void StylePopupPrefab(GameObject root)
    {
        SetImage(root.GetComponent<Image>(), FramedParchment, Parchment, PopupSliceScale);

        // 아래쪽 두 모서리에 금장식 (위쪽은 리본/닫기 버튼 자리라 비움)
        AddCorner(root.transform, "Deco_CornerBL", new Vector2(0, 0), -8, 28, 0);
        AddCorner(root.transform, "Deco_CornerBR", new Vector2(1, 0), -8, 28, 1);

        StyleScrollbars(root.transform);
    }

    private static void StyleDropdownPrefab(GameObject root)
    {
        Image bg = root.GetComponent<Image>();
        SetImage(bg, FillPill, Dark, PillScale(bg));

        Image frame = GetOrCreateDecoImage(root.transform, "Deco_Frame", 0);
        Stretch(frame.rectTransform);
        SetImage(frame, FramePill, Gold, PillScale(bg));

        SetText(root.transform.Find("Label"), CreamText);
        SetColor(FindImage(root.transform, "Arrow"), Gold);

        Transform template = root.transform.Find("Template");
        if (template != null)
        {
            SetColor(template.GetComponent<Image>(), Dark);
            Transform item = FindDeep(template, "Item");
            if (item != null)
            {
                SetColor(FindImage(item, "Item Background"), Leather);
                SetColor(FindImage(item, "Item Checkmark"), Gold);
                SetText(item.Find("Item Label"), CreamText);
            }
        }
        StyleScrollbars(root.transform);
    }

    private static void StyleListItemPrefab(GameObject root)
    {
        SetImage(root.GetComponent<Image>(), FillRound, Dark, ButtonSliceScale);
        SetImage(FindImage(root.transform, "Trim"), FrameRound, Gold, ButtonSliceScale);
        SetText(root.transform.Find("Text (TMP)"), CreamText);
    }

    // ─────────────────────────────────────────────────────────────
    // 씬
    // ─────────────────────────────────────────────────────────────

    /// 씬 인스턴스가 팝업 배경을 오버라이드하고 있어서, 프리팹과 같은 값으로 맞춘다.
    private static void StyleScenePopups(Transform canvas)
    {
        foreach (UIPopup popup in canvas.GetComponentsInChildren<UIPopup>(true))
        {
            Image img = popup.GetComponent<Image>();
            Record(img);
            SetImage(img, FramedParchment, Parchment, PopupSliceScale);
        }
    }

    private static void StylePrimaryButtons(Transform canvas)
    {
        foreach (string name in PrimaryButtons)
        {
            Transform t = FindDeep(canvas, name);
            Image img = t != null ? t.GetComponent<Image>() : null;
            if (img == null)
            {
                Debug.LogWarning($"[UI Theme] 버튼 없음: {name}");
                continue;
            }
            Record(img);
            SetColor(img, Red);
        }
    }

    private static void StyleMainMenu(Transform panel)
    {
        if (panel == null) { Debug.LogWarning("[UI Theme] MainMenuPanel 없음"); return; }

        // 형제 순서 변경까지 Undo 되도록 패널 계층 전체를 기록
        Undo.RegisterFullObjectHierarchyUndo(panel.gameObject, UndoName);

        // 배경: 어두운 단색
        Image bg = panel.GetComponent<Image>();
        Record(bg);
        bg.sprite = null;
        bg.type = Image.Type.Simple;
        bg.color = MenuBackground;

        // 버튼 묶음: 오른쪽으로
        Transform buttons = panel.Find("ButtonRoot");
        if (buttons != null)
        {
            var rt = (RectTransform)buttons;
            Record(rt);
            rt.anchorMin = new Vector2(0.60f, 0.10f);
            rt.anchorMax = new Vector2(0.92f, 0.90f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            var layout = buttons.GetComponent<VerticalLayoutGroup>();
            if (layout != null)
            {
                Record(layout);
                layout.childAlignment = TextAnchor.MiddleCenter;
            }
        }

        // 장식은 모두 버튼 뒤(앞쪽 형제 순서)에 둔다.
        int index = 0;

        // 네 모서리 금장식
        AddCorner(panel, "Deco_CornerBL", new Vector2(0, 0), 16, 64, index++);
        AddCorner(panel, "Deco_CornerBR", new Vector2(1, 0), 16, 64, index++);
        AddCorner(panel, "Deco_CornerTL", new Vector2(0, 1), 16, 64, index++);
        AddCorner(panel, "Deco_CornerTR", new Vector2(1, 1), 16, 64, index++);

        // 타이틀 / 부제 / 구분선 (왼쪽 위)
        TextMeshProUGUI title = GetOrCreateDecoText(panel, "Deco_Title", index++);
        SetAnchors(title.rectTransform, new Vector2(0.07f, 0.62f), new Vector2(0.58f, 0.86f));
        title.text = GameTitle;
        title.fontSize = 56;
        title.fontStyle = FontStyles.Bold;
        title.alignment = TextAlignmentOptions.BottomLeft;
        title.color = CreamText;

        Image divider = GetOrCreateDecoImage(panel, "Deco_Divider", index++);
        SetAnchors(divider.rectTransform, new Vector2(0.07f, 0.60f), new Vector2(0.45f, 0.60f));
        divider.rectTransform.sizeDelta = new Vector2(0, 10);
        SetImage(divider, DividerSprite, Gold, 1f, Image.Type.Simple);

        TextMeshProUGUI subtitle = GetOrCreateDecoText(panel, "Deco_Subtitle", index++);
        SetAnchors(subtitle.rectTransform, new Vector2(0.07f, 0.50f), new Vector2(0.58f, 0.58f));
        subtitle.text = GameSubtitle;
        subtitle.fontSize = 22;
        subtitle.fontStyle = FontStyles.Normal;
        subtitle.alignment = TextAlignmentOptions.TopLeft;
        subtitle.color = Gold;

        // 종족 초상화 라인업 (왼쪽 아래)
        Transform lineup = GetOrCreateDecoRect(panel, "Deco_Lineup", index++);
        SetAnchors((RectTransform)lineup, new Vector2(0.07f, 0.12f), new Vector2(0.55f, 0.46f));
        var hl = lineup.GetComponent<HorizontalLayoutGroup>();
        if (hl == null) hl = AddComp<HorizontalLayoutGroup>(lineup.gameObject);
        else Record(hl);
        hl.spacing = 8;
        hl.childAlignment = TextAnchor.LowerLeft;
        hl.childControlWidth = hl.childControlHeight = true;
        hl.childForceExpandWidth = hl.childForceExpandHeight = true;

        for (int i = 0; i < PortraitRaces.Length; i++)
        {
            string race = PortraitRaces[i];
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{PortraitDir}/Portrait_{race}.png");
            if (sprite == null)
            {
                Debug.LogWarning($"[UI Theme] 초상화 스프라이트 없음: Portrait_{race}.png");
                continue;
            }
            Image portrait = GetOrCreateDecoImage(lineup, $"Deco_Portrait_{race}", i);
            portrait.sprite = sprite;
            portrait.type = Image.Type.Simple;
            portrait.preserveAspect = true;
            portrait.color = Color.white;
        }
    }

    private static void StyleRoster(Transform panel)
    {
        if (panel == null) { Debug.LogWarning("[UI Theme] RosterPanel 없음"); return; }

        Image bg = panel.GetComponent<Image>();
        Record(bg);
        SetColor(bg, Parchment);

        Transform slider = FindDeep(panel, "ChangePointSlider");
        if (slider != null)
        {
            Image back = FindImage(slider, "Background");
            float scale = PillScale(back);
            RecordAndSet(back, FillPill, Dark, scale);
            RecordAndSet(FindImage(slider, "Fill"), FillPill, Red, scale);
            RecordAndSet(FindImage(slider, "Tirm"), FramePill, Gold, scale);

            Image handle = FindImage(slider, "Handle");
            if (handle != null)
            {
                Record(handle);
                handle.sprite = GetSprite(GemSprite);
                handle.preserveAspect = true;
                handle.color = Gold;
            }
        }

        foreach (Scrollbar bar in panel.GetComponentsInChildren<Scrollbar>(true))
        {
            Image barBg = bar.GetComponent<Image>();
            Image barHandle = bar.handleRect != null ? bar.handleRect.GetComponent<Image>() : null;
            if (barBg != null) { Record(barBg); SetColor(barBg, Dark); }
            if (barHandle != null) { Record(barHandle); SetColor(barHandle, Gold); }
        }
    }

    // ─────────────────────────────────────────────────────────────
    // 도우미
    // ─────────────────────────────────────────────────────────────

    private const string UndoName = "Apply UI Theme";

    // 씬 적용 중에만 Undo 를 기록 (프리팹 편집용 임시 오브젝트는 Undo 대상이 아님)
    private static bool sceneMode;

    private static void Record(Object o)
    {
        if (sceneMode && o != null) Undo.RecordObject(o, UndoName);
    }

    private static T AddComp<T>(GameObject go) where T : Component
        => sceneMode ? Undo.AddComponent<T>(go) : go.AddComponent<T>();

    private static Dictionary<string, Sprite> sprites;
    private static TMP_FontAsset font;

    private static bool LoadSprites()
    {
        sprites = AssetDatabase.LoadAllAssetsAtPath(BarsPath)
            .OfType<Sprite>()
            .ToDictionary(s => s.name);
        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);

        if (sprites.Count == 0)
        {
            Debug.LogError($"[UI Theme] 스프라이트를 불러오지 못했습니다: {BarsPath}");
            return false;
        }
        return true;
    }

    private static Sprite GetSprite(string name)
    {
        if (sprites.TryGetValue(name, out Sprite s)) return s;
        Debug.LogWarning($"[UI Theme] 슬라이스 없음: {name}");
        return null;
    }

    private static void SetImage(Image img, string sprite, Color color, float sliceScale, Image.Type type = Image.Type.Sliced)
    {
        if (img == null) return;
        img.sprite = GetSprite(sprite);
        img.type = type;
        img.pixelsPerUnitMultiplier = sliceScale;
        img.color = color;
    }

    private static void RecordAndSet(Image img, string sprite, Color color, float sliceScale)
    {
        if (img == null) return;
        Record(img);
        SetImage(img, sprite, color, sliceScale);
    }

    private static void SetColor(Image img, Color color)
    {
        if (img != null) img.color = color;
    }

    private static void SetText(Transform t, Color color)
    {
        var tmp = t != null ? t.GetComponent<TMP_Text>() : null;
        if (tmp == null) return;
        tmp.color = color;
        if (font != null) tmp.font = font;
    }

    /// 알약형(9-slice 46) 이미지가 높이에 맞게 반원 끝을 갖도록 하는 배율.
    private static float PillScale(Image img)
    {
        float h = img != null ? img.rectTransform.rect.height : 0f;
        return h > 1f ? Mathf.Max(1f, 92f / h) : 3f;
    }

    private static Image FindImage(Transform parent, string name)
    {
        Transform t = FindDeep(parent, name);
        return t != null ? t.GetComponent<Image>() : null;
    }

    private static Transform FindDeep(Transform parent, string name)
    {
        foreach (Transform child in parent.GetComponentsInChildren<Transform>(true))
            if (child != parent && child.name == name) return child;
        return null;
    }

    private static void StyleScrollbars(Transform root)
    {
        foreach (Scrollbar bar in root.GetComponentsInChildren<Scrollbar>(true))
        {
            SetColor(bar.GetComponent<Image>(), Dark);
            if (bar.handleRect != null) SetColor(bar.handleRect.GetComponent<Image>(), Gold);
        }
    }

    // ---- Deco 오브젝트 생성 (이름으로 재사용, 클릭 차단 없음) ----

    private static Transform GetOrCreateDecoRect(Transform parent, string name, int siblingIndex)
    {
        Transform t = parent.Find(name);
        if (t == null)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            if (sceneMode) Undo.RegisterCreatedObjectUndo(go, UndoName);
            go.transform.SetParent(parent, false);
            t = go.transform;
        }
        else
        {
            }
        t.SetSiblingIndex(Mathf.Min(siblingIndex, parent.childCount - 1));
        return t;
    }

    private static Image GetOrCreateDecoImage(Transform parent, string name, int siblingIndex)
    {
        Transform t = GetOrCreateDecoRect(parent, name, siblingIndex);
        Image img = t.GetComponent<Image>();
        if (img == null) img = AddComp<Image>(t.gameObject);
        else Record(img);
        img.raycastTarget = false;
        return img;
    }

    private static TextMeshProUGUI GetOrCreateDecoText(Transform parent, string name, int siblingIndex)
    {
        Transform t = GetOrCreateDecoRect(parent, name, siblingIndex);
        var tmp = t.GetComponent<TextMeshProUGUI>();
        if (tmp == null) tmp = AddComp<TextMeshProUGUI>(t.gameObject);
        else Record(tmp);
        tmp.raycastTarget = false;
        if (font != null) tmp.font = font;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        return tmp;
    }

    /// 모서리 장식. 스프라이트는 왼쪽 아래 기준이라 나머지 모서리는 뒤집어서 쓴다.
    /// anchor 는 (0|1, 0|1), margin 은 안쪽 방향 여백(음수면 바깥으로 삐져나옴).
    private static void AddCorner(Transform parent, string name, Vector2 anchor, float margin, float size, int siblingIndex)
    {
        Image img = GetOrCreateDecoImage(parent, name, siblingIndex);
        RectTransform rt = img.rectTransform;
        float dx = anchor.x < 0.5f ? 1f : -1f;   // 안쪽 방향
        float dy = anchor.y < 0.5f ? 1f : -1f;
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);       // 가운데 기준이라 뒤집어도 위치가 변하지 않음
        rt.anchoredPosition = new Vector2(dx, dy) * (margin + size * 0.5f);
        rt.sizeDelta = new Vector2(size, size);
        rt.localScale = new Vector3(dx, dy, 1f);
        SetImage(img, CornerSprite, Gold, 1f, Image.Type.Simple);
        img.preserveAspect = true;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        rt.localScale = Vector3.one;
    }

    private static void SetAnchors(RectTransform rt, Vector2 min, Vector2 max)
    {
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        rt.localScale = Vector3.one;
    }

    /// 초상화 PNG 를 픽셀아트용(Point, 무압축) 스프라이트로 임포트.
    private static void EnsurePortraitImport()
    {
        foreach (string race in PortraitRaces)
        {
            string path = $"{PortraitDir}/Portrait_{race}.png";
            if (AssetImporter.GetAtPath(path) is not TextureImporter ti) continue;

            bool dirty = ti.textureType != TextureImporterType.Sprite
                         || ti.spriteImportMode != SpriteImportMode.Single
                         || ti.filterMode != FilterMode.Point
                         || ti.textureCompression != TextureImporterCompression.Uncompressed
                         || ti.mipmapEnabled;
            if (!dirty) continue;

            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.filterMode = FilterMode.Point;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.mipmapEnabled = false;
            ti.SaveAndReimport();
        }
    }

    private static Color Rgb(byte r, byte g, byte b) => new Color32(r, g, b, 255);
}
