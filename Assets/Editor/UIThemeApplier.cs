using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
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
///   Tools/UI Theme/3. Apply Polish        : 1.5단계 다듬기만 적용 (프리팹 3개 + 열린 씬)
///   Tools/UI Theme/4. Roster Layout       : 편성 화면 네 단 배치 (열린 씬만, 코드 참조 유지)
///   Tools/UI Theme/5. Battle UI           : 전투 화면·결과 창·팝업 배경·옵션 창·카메라 배경 (열린 씬만)
///     ※ 1·2 를 다시 실행하면 메인 메뉴 ButtonRoot 위치 등이 초기값으로 돌아가니 주의.
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

    // ---- 1.5단계 다듬기 값 ----
    private const string PlainParchment  = "Bars_Unity_3";    // 테두리 없는 양피지
    private const float  PanelFrameScale = 1.5f;              // 편성 패널 금테 굵기 (클수록 얇음)
    private static readonly Vector2 MenuButtonSize = new Vector2(180, 40);
    private const float  MenuButtonFontSize = 20;
    private const float  DropdownHeight     = 34;
    private const float  DropdownFontSize   = 15;
    private const float  DropdownLabelLeft  = 16;             // 라벨 좌측 여백
    private const float  DropdownLabelRight = 32;             // 라벨 우측 여백 (화살표 자리)
    private static readonly Vector2 SliderHandleSize = new Vector2(26, 8); // 손잡이 폭, 세로 여분
    private const float  PopupScrollInsetX      = 20;         // 팝업 본문 좌우 여백
    private const float  PopupScrollInsetBottom = 18;         // 팝업 본문 아래 여백

    // ---- 2-A 편성 화면 네 단 배치 값 ----
    private static readonly Color CardTint = Rgb(230, 211, 174);  // 카드 양피지 (바탕보다 한 톤 어둡게)
    private const float CardSliceScale   = 2f;
    private const float CardTitleSize    = 16;
    private const float CardTitleHeight  = 22;
    private const float ColumnSpacing    = 8;
    private const float TopBarHeight     = 35;
    private const float TopBarTopMargin  = 15;
    private const float RaceWeight = 2, DraftWeight = 3, ListWeight = 3, EnemyWeight = 2;  // 단 너비 비율

    // ---- 3-A 전투 화면 값 ----
    private static readonly Color OverlayDim       = new Color32(12, 9, 7, 170);   // 팝업/결과 뒤 어두운 막
    private static readonly Color CameraBackground = Rgb(23, 18, 14);
    private static readonly Color ResultTitleColor = Rgb(122, 42, 31);              // 양피지 위 승패 문구
    private static readonly Vector2 BattleMainButtonSize  = new Vector2(150, 36);   // 전투 시작 / 턴 종료
    private const float BattleMainButtonFont  = 18;
    private static readonly Vector2 BattleSmallButtonSize = new Vector2(84, 30);    // Option / Abandon
    private const float BattleSmallButtonFont = 14;
    private const float BattleTopMargin       = 12;
    private const float TrayHeight            = 64;    // 하단 행동 받침
    private const float TrayBottomMargin      = 14;
    private const float TraySliceScale        = 2f;
    private static readonly Vector2 LogSize   = new Vector2(210, 270);
    private const float LogMargin             = 10;
    private const float LogFontSize           = 11;
    private static readonly Vector2 ResultCardSize = new Vector2(360, 220);
    private const string LogTitle      = "전투 기록";
    private const string SoundTitle    = "소리";
    private const string EndTurnLabel  = "End Turn";
    private const string AbandonLabel  = "Abandon";
    private const string CancelLabel   = "Cancel";
    private const string LogButtonName  = "UIButton_Log";
    private const string LogButtonLabel = "Log";

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

    /// 1·2 단계 이후 다듬기만 적용 (메인 메뉴 배치·Canvas 등 사용자가 손본 부분은 건드리지 않음).
    [MenuItem("Tools/UI Theme/3. Apply Polish (Prefabs + Open Scene)")]
    private static void ApplyPolishMenu()
    {
        if (!EditorUtility.DisplayDialog("UI Theme",
                "UIPopupScroll / UIDropDown / UILabeledButton 프리팹을 덮어쓰고 열린 씬을 수정합니다.\n프리팹 저장은 Undo 로 되돌릴 수 없으니, 먼저 커밋해 두세요.",
                "적용", "취소"))
            return;

        if (!LoadSprites()) return;

        // 프리팹
        EditPrefab("UIPopupScroll", root => InsetPopupScrollViews(root.transform));
        EditPrefab("UIDropDown", root => StyleDropdownLabel(root.GetComponent<TMP_Dropdown>()));
        EditPrefab("UILabeledButton", root => SetText(root.transform.Find("Label"), BrownText));
        AssetDatabase.SaveAssets();

        // 씬
        Scene scene = SceneManager.GetActiveScene();
        Canvas canvas = scene.GetRootGameObjects()
            .Select(go => go.GetComponent<Canvas>())
            .FirstOrDefault(c => c != null);
        if (canvas == null)
        {
            Debug.LogError("[UI Theme] 열린 씬의 루트에서 Canvas 를 찾지 못했습니다. PlayScene 을 열고 실행하세요.");
            return;
        }

        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName(UndoName);
        int group = Undo.GetCurrentGroup();
        sceneMode = true;

        Transform root = canvas.transform;
        foreach (UIPopup popup in root.GetComponentsInChildren<UIPopup>(true))
            InsetPopupScrollViews(popup.transform);
        PolishMainMenuButtons(root.Find("MainMenuPanel"));
        PolishRoster(root.Find("RosterPanel"));

        sceneMode = false;
        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log("[UI Theme] 다듬기 적용 완료 (저장은 Ctrl+S 로 직접, 되돌리기는 Ctrl+Z)");
    }

    /// 편성 화면을 종족 | 유닛 구성 | 편성 목록 | 적 설정 네 단으로 재배치 (씬만 수정, 코드 참조 유지).
    [MenuItem("Tools/UI Theme/4. Roster Layout (Open Scene)")]
    private static void RosterLayoutMenu()
    {
        if (!LoadSprites()) return;

        Scene scene = SceneManager.GetActiveScene();
        Canvas canvas = scene.GetRootGameObjects()
            .Select(go => go.GetComponent<Canvas>())
            .FirstOrDefault(c => c != null);
        Transform panel = canvas != null ? canvas.transform.Find("RosterPanel") : null;
        if (panel == null)
        {
            Debug.LogError("[UI Theme] RosterPanel 을 찾지 못했습니다. PlayScene 을 열고 실행하세요.");
            return;
        }

        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName(UndoName);
        int group = Undo.GetCurrentGroup();
        sceneMode = true;
        try
        {
            LayoutRosterColumns(panel);
        }
        finally
        {
            sceneMode = false;
        }
        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log("[UI Theme] 편성 화면 네 단 배치 완료 (저장은 Ctrl+S 로 직접, 되돌리기는 Ctrl+Z)");
    }

    /// 전투 화면(상단 버튼, 하단 행동 받침, 전투 로그), 결과 창, 팝업 배경, 옵션 창, 카메라 배경 (씬만 수정).
    [MenuItem("Tools/UI Theme/5. Battle UI (Open Scene)")]
    private static void BattleUIMenu()
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

        Transform root = canvas.transform;
        Transform battle = root.Find("BattlePanel");
        Transform result = root.Find("BattleResultPanel");
        Transform popups = root.Find("PopupPanel");

        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName(UndoName);
        int group = Undo.GetCurrentGroup();
        sceneMode = true;
        try
        {
            // 형제 순서·오브젝트 추가까지 한 번에 되돌릴 수 있게 계층 전체를 기록
            foreach (Transform t in new[] { battle, result, popups })
                if (t != null) Undo.RegisterFullObjectHierarchyUndo(t.gameObject, UndoName);

            if (battle != null)
            {
                StyleBattleTopButtons(battle);
                StyleActionTray(battle);
                StyleBattleLog(battle);
                SetupLogToggle(battle);
            }
            else Debug.LogWarning("[UI Theme] BattlePanel 없음");

            StyleResultPanel(result);
            StylePopupLayer(popups);
            StyleCameraBackground(scene);
        }
        finally
        {
            sceneMode = false;
        }
        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log("[UI Theme] 전투 UI 적용 완료 (저장은 Ctrl+S 로 직접, 되돌리기는 Ctrl+Z)");
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
    // 1.5단계 다듬기
    // ─────────────────────────────────────────────────────────────

    /// 새 팝업 배경(_20)은 테두리가 두꺼워서, 본문 스크롤 영역을 안쪽으로 들인다.
    /// 이미 여백이 더 큰 쪽은 그대로 둔다.
    private static void InsetPopupScrollViews(Transform popup)
    {
        foreach (ScrollRect scroll in popup.GetComponentsInChildren<ScrollRect>(true))
        {
            var rt = (RectTransform)scroll.transform;
            Record(rt);
            Vector2 min = rt.offsetMin, max = rt.offsetMax;
            if (Mathf.Approximately(rt.anchorMin.x, 0) && Mathf.Approximately(rt.anchorMax.x, 1))
            {
                min.x = Mathf.Max(min.x, PopupScrollInsetX);
                max.x = Mathf.Min(max.x, -PopupScrollInsetX);
            }
            if (Mathf.Approximately(rt.anchorMin.y, 0))
                min.y = Mathf.Max(min.y, PopupScrollInsetBottom);
            rt.offsetMin = min;
            rt.offsetMax = max;
        }
    }

    private static void StyleDropdownLabel(TMP_Dropdown dropdown)
    {
        if (dropdown == null || dropdown.captionText == null) return;
        TMP_Text label = dropdown.captionText;
        RectTransform rt = label.rectTransform;
        Record(label);
        Record(rt);
        label.fontSize = DropdownFontSize;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        rt.offsetMin = new Vector2(DropdownLabelLeft, rt.offsetMin.y);
        rt.offsetMax = new Vector2(-DropdownLabelRight, rt.offsetMax.y);
    }

    private static void PolishMainMenuButtons(Transform panel)
    {
        Transform buttons = panel != null ? panel.Find("ButtonRoot") : null;
        if (buttons == null) { Debug.LogWarning("[UI Theme] MainMenuPanel/ButtonRoot 없음"); return; }

        foreach (Transform child in buttons)
        {
            if (child.GetComponent<UIButton>() == null) continue;
            var rt = (RectTransform)child;
            Record(rt);
            rt.sizeDelta = MenuButtonSize;   // 위치는 건드리지 않음 (직접 옮긴 Credit 유지)

            Transform textT = child.Find("Text (TMP)");
            TMP_Text text = textT != null ? textT.GetComponent<TMP_Text>() : null;
            if (text != null)
            {
                Record(text);
                text.fontSize = MenuButtonFontSize;
            }
        }
    }

    private static void PolishRoster(Transform panel)
    {
        if (panel == null) { Debug.LogWarning("[UI Theme] RosterPanel 없음"); return; }

        // 1) 바깥 테두리: 테두리 없는 양피지 + 금테 (맨 위에 그리되 클릭은 통과)
        Image bg = panel.GetComponent<Image>();
        Record(bg);
        SetImage(bg, PlainParchment, Parchment, 1f);

        Image frame = GetOrCreateDecoImage(panel, "Deco_GoldFrame", int.MaxValue);
        Stretch(frame.rectTransform);
        SetImage(frame, FrameRound, Gold, PanelFrameScale);

        // 2) 상단: 포인트 라벨이 종족 버튼 줄과 겹치지 않게 아래쪽 25px 안으로
        Transform pointsT = FindDeep(panel, "PointsWeaponLabel");
        TMP_Text points = pointsT != null ? pointsT.GetComponent<TMP_Text>() : null;
        if (points != null)
        {
            RectTransform rt = points.rectTransform;
            Record(rt);
            Record(points);
            rt.anchoredPosition = new Vector2(10, 0);
            rt.sizeDelta = new Vector2(Mathf.Max(rt.sizeDelta.x, 200), 25);
            points.alignment = TextAlignmentOptions.MidlineLeft;
        }

        // 3) 슬라이더 손잡이 키우기
        Transform slider = FindDeep(panel, "ChangePointSlider");
        Image handle = slider != null ? FindImage(slider, "Handle") : null;
        if (handle != null)
        {
            Record(handle.rectTransform);
            handle.rectTransform.sizeDelta = SliderHandleSize;
        }

        // 4) 장비 라벨 색 (프리팹과 같은 값으로 맞춤)
        foreach (UILabledButton labeled in panel.GetComponentsInChildren<UILabledButton>(true))
        {
            Transform labelT = labeled.transform.Find("Label");
            TMP_Text label = labelT != null ? labelT.GetComponent<TMP_Text>() : null;
            if (label == null) continue;
            Record(label);
            label.color = BrownText;
        }

        // 5) 오른쪽 열: 드롭다운은 알약 높이로, 적 종족 라벨은 한 줄 높이로
        Transform right = FindDeep(panel, "RightColumn");
        if (right == null) return;

        var layout = right.GetComponent<VerticalLayoutGroup>();
        if (layout != null)
        {
            Record(layout);
            layout.childForceExpandHeight = false;   // 남는 공간은 flexible 인 종족 버튼 묶음이 가져감
        }

        float pillScale = Mathf.Max(1f, 92f / DropdownHeight);
        foreach (TMP_Dropdown dropdown in right.GetComponentsInChildren<TMP_Dropdown>(true))
        {
            SetPreferredHeight(dropdown.gameObject, DropdownHeight);
            var rt = (RectTransform)dropdown.transform;
            Record(rt);
            rt.sizeDelta = new Vector2(rt.sizeDelta.x, DropdownHeight);

            Image ddBg = dropdown.GetComponent<Image>();
            if (ddBg != null) { Record(ddBg); ddBg.pixelsPerUnitMultiplier = pillScale; }
            Transform ddFrameT = dropdown.transform.Find("Deco_Frame");
            Image ddFrame = ddFrameT != null ? ddFrameT.GetComponent<Image>() : null;
            if (ddFrame != null) { Record(ddFrame); ddFrame.pixelsPerUnitMultiplier = pillScale; }

            StyleDropdownLabel(dropdown);
        }

        Transform aiLabel = right.Find("AIRaceLabel");
        if (aiLabel != null)
        {
            SetPreferredHeight(aiLabel.gameObject, 22);
            TMP_Text tmp = aiLabel.GetComponent<TMP_Text>();
            if (tmp != null)
            {
                Record(tmp);
                tmp.fontSize = 14;
                tmp.color = BrownText;
            }
        }
    }

    // ─────────────────────────────────────────────────────────────
    // 2-A 편성 화면 네 단 배치
    // ─────────────────────────────────────────────────────────────

    private static void LayoutRosterColumns(Transform panel)
    {
        Transform topBar      = FindDeep(panel, "TopBar");
        Transform row         = FindDeep(panel, "MainContentRow");
        Transform raceButtons = FindDeep(panel, "RaceButtonContainter");
        Transform draft       = FindDeep(panel, "DraftPanel");
        Transform list        = FindDeep(panel, "RosterListContainer");
        Transform right       = FindDeep(panel, "RightColumn");
        Transform leftColumn  = FindDeep(panel, "LeftColumn");

        if (topBar == null || row == null || raceButtons == null || draft == null || list == null || right == null)
        {
            Debug.LogError("[UI Theme] 편성 화면 오브젝트를 모두 찾지 못했습니다 (TopBar / MainContentRow / RaceButtonContainter / DraftPanel / RosterListContainer / RightColumn).");
            return;
        }

        // 형제 순서·부모 변경까지 Ctrl+Z 로 되돌릴 수 있게 패널 계층 전체를 기록
        Undo.RegisterFullObjectHierarchyUndo(panel.gameObject, UndoName);

        // 1) 상단 바: 포인트 라벨 + 슬라이더만 남기고 높이 축소
        var topRt = (RectTransform)topBar;
        Record(topRt);
        topRt.sizeDelta = new Vector2(topRt.sizeDelta.x, TopBarHeight);

        Transform slider = FindDeep(topBar, "ChangePointSlider");
        if (slider != null)
        {
            var srt = (RectTransform)slider;
            Record(srt);
            srt.anchoredPosition = new Vector2(srt.anchoredPosition.x, 0);
        }
        Transform points = FindDeep(topBar, "PointsWeaponLabel");
        if (points != null)
        {
            var prt = (RectTransform)points;
            Record(prt);
            prt.anchoredPosition = new Vector2(prt.anchoredPosition.x, (TopBarHeight - prt.sizeDelta.y) * 0.5f);
        }

        // 2) 본문 줄: 상단 바가 줄어든 만큼 위로 늘리고 단 사이 간격
        var rowRt = (RectTransform)row;
        Record(rowRt);
        rowRt.offsetMax = new Vector2(rowRt.offsetMax.x, -(TopBarTopMargin + TopBarHeight + 5));
        var rowLayout = row.GetComponent<HorizontalLayoutGroup>();
        if (rowLayout != null)
        {
            Record(rowLayout);
            rowLayout.spacing = ColumnSpacing;
            rowLayout.childControlWidth = rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = rowLayout.childForceExpandHeight = true;
        }

        // 3) 카드 세 장 만들고 기존 패널을 옮겨 담기
        Transform raceCard  = GetOrCreateCard(row, "Card_Race", "종족", RaceWeight, 0);
        Transform draftCard = GetOrCreateCard(row, "Card_Draft", "유닛 구성", DraftWeight, 1);
        Transform listCard  = GetOrCreateCard(row, "Card_List", "편성 목록", ListWeight, 2);

        MoveInto(raceButtons, raceCard);
        MoveInto(draft, draftCard);
        MoveInto(list, listCard);

        // 종족 버튼: 가로 → 세로 쌓기 (LayoutGroup 은 한 오브젝트에 하나만 가능)
        var hlg = raceButtons.GetComponent<HorizontalLayoutGroup>();
        if (hlg != null) Undo.DestroyObjectImmediate(hlg);
        var vlg = raceButtons.GetComponent<VerticalLayoutGroup>();
        if (vlg == null) vlg = AddComp<VerticalLayoutGroup>(raceButtons.gameObject);
        else Record(vlg);
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.spacing = 6;
        vlg.padding = new RectOffset(0, 0, 4, 4);
        vlg.childControlWidth = vlg.childControlHeight = false;    // 버튼은 프리팹 크기 유지
        vlg.childForceExpandWidth = vlg.childForceExpandHeight = false;

        // 안쪽 회색 사각 테두리는 투명하게 (Image 는 남겨서 스크롤 드래그 유지)
        HideImage(draft.GetComponent<Image>());
        HideImage(list.GetComponent<Image>());
        FixRosterListScroll(list);

        // 4) 적 설정: RightColumn 자체를 카드로 쓰고 맨 위에 제목
        Image rightImg = right.GetComponent<Image>();
        Record(rightImg);
        SetImage(rightImg, FramedParchment, CardTint, CardSliceScale);
        SetLayoutElement(right.gameObject, flexibleWidth: EnemyWeight);
        TextMeshProUGUI enemyTitle = MakeCardTitle(right, "적 설정");
        enemyTitle.rectTransform.sizeDelta = new Vector2(130, CardTitleHeight);   // 이 열은 폭을 자식이 정함
        if (right.parent != row) Undo.SetTransformParent(right, row, UndoName);
        right.SetSiblingIndex(3);

        // 5) 비어 버린 LeftColumn 은 지우지 않고 끄기만
        if (leftColumn != null && leftColumn.childCount == 0 && leftColumn.gameObject.activeSelf)
        {
            Undo.RecordObject(leftColumn.gameObject, UndoName);
            leftColumn.gameObject.SetActive(false);
            leftColumn.SetAsLastSibling();
        }
    }

    private static Transform GetOrCreateCard(Transform row, string name, string title, float weight, int index)
    {
        Image img = GetOrCreateDecoImage(row, name, index);
        SetImage(img, FramedParchment, CardTint, CardSliceScale);

        GameObject go = img.gameObject;
        var layout = go.GetComponent<VerticalLayoutGroup>();
        if (layout == null) layout = AddComp<VerticalLayoutGroup>(go);
        else Record(layout);
        layout.padding = new RectOffset(10, 10, 10, 10);
        layout.spacing = 6;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        SetLayoutElement(go, flexibleWidth: weight);
        go.GetComponent<LayoutElement>().minWidth = 0;   // 내용물 최소 폭이 비율을 깨지 않게
        MakeCardTitle(go.transform, title);
        return go.transform;
    }

    /// 편성 목록: 가로 스크롤 끄기 + 내용물을 뷰포트 폭에 맞추고 세로로 늘어나게.
    private static void FixRosterListScroll(Transform list)
    {
        var sr = list.GetComponent<ScrollRect>();
        if (sr == null) return;
        Record(sr);
        sr.horizontal = false;
        if (sr.horizontalScrollbar != null)
        {
            GameObject bar = sr.horizontalScrollbar.gameObject;
            sr.horizontalScrollbar = null;
            Undo.RecordObject(bar, UndoName);
            bar.SetActive(false);
        }

        RectTransform content = sr.content;
        if (content == null) return;
        var fitter = content.GetComponent<ContentSizeFitter>();
        if (fitter != null)
        {
            Record(fitter);
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }
        Record(content);
        content.anchorMin = new Vector2(0, 1);
        content.anchorMax = new Vector2(1, 1);
        content.pivot = new Vector2(0.5f, 1);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = new Vector2(0, content.sizeDelta.y);
        var grid = content.GetComponent<GridLayoutGroup>();
        if (grid != null)
        {
            Record(grid);
            grid.childAlignment = TextAnchor.UpperCenter;
        }
    }

    private static TextMeshProUGUI MakeCardTitle(Transform card, string text)
    {
        TextMeshProUGUI title = GetOrCreateDecoText(card, "Deco_Title", 0);
        title.text = text;
        title.fontSize = CardTitleSize;
        title.fontStyle = FontStyles.Bold;
        title.alignment = TextAlignmentOptions.Center;
        title.color = BrownText;
        SetPreferredHeight(title.gameObject, CardTitleHeight);
        return title;
    }

    /// 카드 안으로 옮기고, 남은 세로 공간을 채우게 한다.
    private static void MoveInto(Transform child, Transform card)
    {
        if (child.parent != card)
            Undo.SetTransformParent(child, card, UndoName);
        Record(child);
        child.localScale = Vector3.one;
        child.SetAsLastSibling();   // 제목 다음
        SetLayoutElement(child.gameObject, flexibleHeight: 1);
    }

    private static void HideImage(Image img)
    {
        if (img == null) return;
        Record(img);
        Color c = img.color;
        c.a = 0;
        img.color = c;
    }

    private static void SetLayoutElement(GameObject go, float? flexibleWidth = null, float? flexibleHeight = null)
    {
        var le = go.GetComponent<LayoutElement>();
        if (le == null) le = AddComp<LayoutElement>(go);
        else Record(le);
        if (flexibleWidth.HasValue)
        {
            le.preferredWidth = 0;   // 내용물 폭과 무관하게 비율(flexible)대로만 나눔
            le.flexibleWidth = flexibleWidth.Value;
        }
        if (flexibleHeight.HasValue) le.flexibleHeight = flexibleHeight.Value;
    }

    private static void SetPreferredHeight(GameObject go, float height)
    {
        var le = go.GetComponent<LayoutElement>();
        if (le == null) le = AddComp<LayoutElement>(go);
        else Record(le);
        le.preferredHeight = height;
        le.flexibleHeight = 0;
    }

    // ─────────────────────────────────────────────────────────────
    // 3-A 전투 화면
    // ─────────────────────────────────────────────────────────────

    private static readonly Vector2 Center = new Vector2(0.5f, 0.5f);

    /// 상단 가운데 전투 시작/턴 종료 (빨간 강조), 오른쪽 위 Option/Abandon.
    private static void StyleBattleTopButtons(Transform battle)
    {
        foreach (string name in new[] { "UIButton_Start", "UIButton_EndTurn" })
        {
            Transform t = FindDeep(battle, name);
            if (t == null) { Debug.LogWarning($"[UI Theme] 버튼 없음: {name}"); continue; }
            SetRect((RectTransform)t, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1),
                    new Vector2(0, -BattleTopMargin), BattleMainButtonSize);
            Image img = t.GetComponent<Image>();
            Record(img);
            SetColor(img, Red);
            // 턴 종료 버튼 라벨이 "Start" 로 되어 있던 것 수정
            SetButtonLabel(t, name == "UIButton_EndTurn" ? EndTurnLabel : null, BattleMainButtonFont);
        }

        Transform rootT = FindDeep(battle, "BattleOptionButtonRoot");
        if (rootT == null) { Debug.LogWarning("[UI Theme] BattleOptionButtonRoot 없음"); return; }
        var rootRt = (RectTransform)rootT;
        Record(rootRt);
        rootRt.sizeDelta = new Vector2(rootRt.sizeDelta.x, BattleSmallButtonSize.y + BattleTopMargin);

        var layout = rootT.GetComponent<HorizontalLayoutGroup>();
        if (layout != null)
        {
            Record(layout);
            layout.padding = new RectOffset(0, (int)BattleTopMargin, (int)BattleTopMargin, 0);
            layout.spacing = 8;
            layout.childAlignment = TextAnchor.UpperRight;
        }

        foreach (Transform child in rootT)
        {
            if (child.GetComponent<Button>() == null) continue;
            var rt = (RectTransform)child;
            Record(rt);
            rt.sizeDelta = BattleSmallButtonSize;
            SetButtonLabel(child, child.name == "UIButton_Abadon" ? AbandonLabel : null, BattleSmallButtonFont);
        }
    }

    /// 화면 폭 전체를 덮던 회색 띠 → 아래 가운데, 버튼 수에 맞춰 늘어나는 가죽 받침 + 금테.
    private static void StyleActionTray(Transform battle)
    {
        Transform tray = FindDeep(battle, "OptionButtonContainer");
        if (tray == null) { Debug.LogWarning("[UI Theme] OptionButtonContainer 없음"); return; }

        SetRect((RectTransform)tray, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0),
                new Vector2(0, TrayBottomMargin), new Vector2(200, TrayHeight));

        Image bg = tray.GetComponent<Image>();
        RecordAndSet(bg, FillRound, Dark, TraySliceScale);

        var layout = tray.GetComponent<HorizontalLayoutGroup>();
        if (layout != null)
        {
            Record(layout);
            layout.padding = new RectOffset(24, 24, 12, 12);
            layout.spacing = 10;
            layout.childAlignment = TextAnchor.MiddleCenter;
        }

        var fitter = GetOrAdd<ContentSizeFitter>(tray.gameObject);
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

        Image frame = GetOrCreateDecoImage(tray, "Deco_Frame", 0);
        Stretch(frame.rectTransform);
        SetImage(frame, FrameRound, Gold, TraySliceScale);
        IgnoreLayout(frame.gameObject);

        // 닫기 버튼은 받침 오른쪽 위 모서리에 걸치게
        Transform close = tray.Find("UIButton_Close");
        if (close != null)
        {
            IgnoreLayout(close.gameObject);
            SetRect((RectTransform)close, Vector2.one, Vector2.one, Center, new Vector2(-4, -4), new Vector2(26, 26));
            close.SetAsLastSibling();
        }
    }

    /// 오른쪽 전투 로그: 크기 키우고 제목, 읽을 수 있는 글자, 내용에 맞춰 늘어나는 스크롤.
    private static void StyleBattleLog(Transform battle)
    {
        Transform log = FindDeep(battle, "BattleLog");
        if (log == null) { Debug.LogWarning("[UI Theme] BattleLog 없음"); return; }

        SetRect((RectTransform)log, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f),
                new Vector2(-LogMargin, 0), LogSize);
        RecordAndSet(log.GetComponent<Image>(), FramedParchment, Parchment, PopupSliceScale);

        TextMeshProUGUI title = GetOrCreateDecoText(log, "Deco_Title", 0);
        RectTransform titleRt = title.rectTransform;
        titleRt.anchorMin = new Vector2(0, 1);
        titleRt.anchorMax = new Vector2(1, 1);
        titleRt.pivot = new Vector2(0.5f, 1);
        titleRt.anchoredPosition = new Vector2(0, -12);
        titleRt.sizeDelta = new Vector2(-60, 22);          // 오른쪽 닫기 버튼 자리 비움
        titleRt.localScale = Vector3.one;
        title.text = LogTitle;
        title.fontSize = 15;
        title.fontStyle = FontStyles.Bold;
        title.alignment = TextAlignmentOptions.Center;
        title.color = BrownText;

        Transform scrollT = log.Find("Scroll View (1)");
        ScrollRect scroll = scrollT != null ? scrollT.GetComponent<ScrollRect>() : null;
        if (scroll != null)
        {
            var rt = (RectTransform)scrollT;
            Record(rt);
            rt.offsetMin = new Vector2(16, 16);
            rt.offsetMax = new Vector2(-16, -38);
            StyleScrollbarsRecorded(scrollT);

            RectTransform content = scroll.content;
            if (content != null)
            {
                // 글이 늘어나면 Content 도 늘어나야 스크롤이 된다
                var vlg = GetOrAdd<VerticalLayoutGroup>(content.gameObject);
                vlg.padding = new RectOffset(2, 2, 2, 2);
                vlg.childControlWidth = vlg.childControlHeight = true;
                vlg.childForceExpandWidth = true;
                vlg.childForceExpandHeight = false;

                var fitter = GetOrAdd<ContentSizeFitter>(content.gameObject);
                fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                Record(content);
                content.anchorMin = new Vector2(0, 1);
                content.anchorMax = new Vector2(1, 1);
                content.pivot = new Vector2(0.5f, 1);
                content.anchoredPosition = Vector2.zero;
                content.sizeDelta = new Vector2(0, content.sizeDelta.y);

                foreach (TMP_Text text in content.GetComponentsInChildren<TMP_Text>(true))
                {
                    Record(text);
                    text.fontSize = LogFontSize;
                    text.color = BrownText;
                    text.alignment = TextAlignmentOptions.TopLeft;
                    text.textWrappingMode = TextWrappingModes.Normal;
                    if (font != null) text.font = font;
                }
            }
        }

        Transform close = log.Find("CloseButton");
        if (close != null)
        {
            SetRect((RectTransform)close, Vector2.one, Vector2.one, Center, new Vector2(-16, -16), new Vector2(22, 22));
            SetButtonLabel(close, null, 14);
            close.SetAsLastSibling();
        }
    }

    /// 로그 X 버튼 → BattleLog 끄기, 오른쪽 위 Log 버튼 → 다시 켜기.
    /// 런타임 코드 없이 Button.onClick 에 GameObject.SetActive 를 씬에 직접 연결한다.
    private static void SetupLogToggle(Transform battle)
    {
        Transform log = FindDeep(battle, "BattleLog");
        Transform rootT = FindDeep(battle, "BattleOptionButtonRoot");
        if (log == null || rootT == null) { Debug.LogWarning("[UI Theme] BattleLog / BattleOptionButtonRoot 없음"); return; }
        GameObject logGo = log.gameObject;

        Transform close = log.Find("CloseButton");
        if (close != null) BindSetActive(close.GetComponent<Button>(), logGo, false);

        Transform open = rootT.Find(LogButtonName);
        if (open == null)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabDir}/UIButton.prefab");
            if (prefab == null) { Debug.LogWarning("[UI Theme] UIButton 프리팹 없음"); return; }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, rootT);
            Undo.RegisterCreatedObjectUndo(go, UndoName);
            go.name = LogButtonName;
            open = go.transform;
        }
        open.SetSiblingIndex(0);   // Option / Abandon 왼쪽
        Record(open.gameObject);
        open.gameObject.SetActive(true);
        var openRt = (RectTransform)open;
        Record(openRt);
        openRt.sizeDelta = BattleSmallButtonSize;
        openRt.localScale = Vector3.one;
        SetButtonLabel(open, LogButtonLabel, BattleSmallButtonFont);
        BindSetActive(open.GetComponent<Button>(), logGo, true);
    }

    /// button 클릭 시 target.SetActive(value). 이전에 연결해 둔 같은 호출은 지우고 다시 건다 (재실행 안전).
    private static void BindSetActive(Button button, GameObject target, bool value)
    {
        if (button == null) return;
        Undo.RecordObject(button, UndoName);
        for (int i = button.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
        {
            if (button.onClick.GetPersistentTarget(i) == target
                && button.onClick.GetPersistentMethodName(i) == nameof(GameObject.SetActive))
                UnityEventTools.RemovePersistentListener(button.onClick, i);
        }
        UnityEventTools.AddBoolPersistentListener(button.onClick, target.SetActive, value);
        EditorUtility.SetDirty(button);
    }

    /// 결과 창: 어두운 막 + 가운데 양피지 카드, 굵은 승패 문구, 빨간 Menu 버튼.
    private static void StyleResultPanel(Transform panel)
    {
        if (panel == null) { Debug.LogWarning("[UI Theme] BattleResultPanel 없음"); return; }

        SetDim(panel.GetComponent<Image>());

        Image card = GetOrCreateDecoImage(panel, "Deco_Card", 0);
        RectTransform cardRt = card.rectTransform;
        cardRt.anchorMin = cardRt.anchorMax = cardRt.pivot = Center;
        cardRt.anchoredPosition = Vector2.zero;
        cardRt.sizeDelta = ResultCardSize;
        cardRt.localScale = Vector3.one;
        SetImage(card, FramedParchment, Parchment, PopupSliceScale);
        AddCorner(card.transform, "Deco_CornerBL", new Vector2(0, 0), -8, 32, 0);
        AddCorner(card.transform, "Deco_CornerBR", new Vector2(1, 0), -8, 32, 1);

        Transform winT = panel.Find("Win Text (TMP)");
        TMP_Text win = winT != null ? winT.GetComponent<TMP_Text>() : null;
        if (win != null)
        {
            SetRect(win.rectTransform, Center, Center, Center, new Vector2(0, 30), new Vector2(300, 70));
            Record(win);
            win.fontSize = 44;
            win.fontStyle = FontStyles.Bold;
            win.alignment = TextAlignmentOptions.Center;
            win.color = ResultTitleColor;
            if (font != null) win.font = font;
        }

        // 프리팹이 아닌 기본 버튼이라 UIButton 과 같은 모양을 직접 입힌다
        Transform button = panel.Find("ConfirmButton");
        if (button != null)
        {
            SetRect((RectTransform)button, Center, Center, Center, new Vector2(0, -55), new Vector2(150, 38));
            RecordAndSet(button.GetComponent<Image>(), FillRound, Red, ButtonSliceScale);

            Image trim = GetOrCreateDecoImage(button, "Deco_Trim", 0);   // 글자 뒤
            Stretch(trim.rectTransform);
            SetImage(trim, FrameRound, Gold, ButtonSliceScale);

            SetButtonLabel(button, null, 18);
        }
    }

    /// 팝업 공통 어두운 막 + 종료 확인 창 버튼 + 옵션 창.
    private static void StylePopupLayer(Transform popups)
    {
        if (popups == null) { Debug.LogWarning("[UI Theme] PopupPanel 없음"); return; }

        SetDim(popups.GetComponent<Image>());

        Transform confirmPopup = popups.Find("UIPopup");
        if (confirmPopup != null)
        {
            Transform confirm = FindDeep(confirmPopup, "ConfirmButton");
            if (confirm != null)
            {
                Image img = confirm.GetComponent<Image>();
                Record(img);
                SetColor(img, Red);
            }
            Transform cancel = FindDeep(confirmPopup, "CancleButton");
            if (cancel != null) SetLabelText(cancel, CancelLabel);
        }

        Transform options = popups.Find("OptionPopup");
        Transform row = options != null ? FindDeep(options, "OptionElement") : null;
        if (row != null) StyleVolumeRow(row);
        else Debug.LogWarning("[UI Theme] OptionPopup/OptionElement 없음");
    }

    /// 옵션 창: "소리" 제목 + 구분선, 볼륨 줄을 편성 화면 슬라이더와 같은 알약 모양으로.
    private static void StyleVolumeRow(Transform row)
    {
        Transform content = row.parent;

        // Content 에는 씬에서 추가한 HorizontalLayoutGroup 이 있어 제목·구분선·줄이 옆으로 늘어선다.
        // 세로 배치로 바꾼다 (LayoutGroup 은 한 오브젝트에 하나만 가능 → 먼저 제거).
        var hlg = content.GetComponent<HorizontalLayoutGroup>();
        if (hlg != null) Undo.DestroyObjectImmediate(hlg);
        var vlg = GetOrAdd<VerticalLayoutGroup>(content.gameObject);
        vlg.padding = new RectOffset(8, 8, 6, 6);
        vlg.spacing = 6;
        vlg.childAlignment = TextAnchor.UpperLeft;
        vlg.childControlWidth = vlg.childControlHeight = true;
        vlg.childForceExpandWidth = vlg.childForceExpandHeight = false;

        TextMeshProUGUI title = GetOrCreateDecoText(content, "Deco_SoundTitle", 0);
        title.rectTransform.localScale = Vector3.one;
        title.text = SoundTitle;
        title.fontSize = 18;
        title.fontStyle = FontStyles.Bold;
        title.alignment = TextAlignmentOptions.MidlineLeft;
        title.color = BrownText;
        SetPreferredHeight(title.gameObject, 26);
        SetFlexibleWidth(title.gameObject, 1);

        Image divider = GetOrCreateDecoImage(content, "Deco_SoundDivider", 1);
        divider.rectTransform.localScale = Vector3.one;
        SetImage(divider, DividerSprite, Gold, 1f, Image.Type.Simple);
        SetPreferredHeight(divider.gameObject, 9);
        var divLe = divider.GetComponent<LayoutElement>();
        divLe.preferredWidth = 160;
        divLe.flexibleWidth = 0;

        Record(row);
        row.SetSiblingIndex(2);
        row.localScale = Vector3.one;
        SetPreferredHeight(row.gameObject, 40);
        SetFlexibleWidth(row.gameObject, 1);
        HideImage(row.GetComponent<Image>());

        Transform labelT = row.Find("OptionTitle");
        TMP_Text label = labelT != null ? labelT.GetComponent<TMP_Text>() : null;
        if (label != null)
        {
            Record(label);
            label.fontSize = 16;
            label.color = BrownText;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            if (font != null) label.font = font;
        }

        Slider slider = row.GetComponentInChildren<Slider>(true);
        if (slider == null) return;

        const float left = 92, right = 8, height = 28;
        var sRt = (RectTransform)slider.transform;
        Record(sRt);
        sRt.anchorMin = new Vector2(0, 0.5f);
        sRt.anchorMax = new Vector2(1, 0.5f);
        sRt.pivot = Center;
        sRt.anchoredPosition = new Vector2((left - right) * 0.5f, 0);
        sRt.sizeDelta = new Vector2(-(left + right), height);

        // 막대 두께 = 높이의 60%, 알약 끝이 반원이 되도록 배율 계산
        const float barMin = 0.2f, barMax = 0.8f;
        float pill = Mathf.Max(1f, 92f / (height * (barMax - barMin)));
        foreach (string areaName in new[] { "Background", "Fill Area" })
        {
            Transform area = slider.transform.Find(areaName);
            if (area == null) continue;
            var aRt = (RectTransform)area;
            Record(aRt);
            aRt.anchorMin = new Vector2(aRt.anchorMin.x, barMin);
            aRt.anchorMax = new Vector2(aRt.anchorMax.x, barMax);
        }
        RecordAndSet(FindImage(slider.transform, "Background"), FillPill, Dark, pill);
        RecordAndSet(FindImage(slider.transform, "Fill"), FillPill, Red, pill);
        RecordAndSet(FindImage(slider.transform, "Trim"), FramePill, Gold, pill);

        Image handle = FindImage(slider.transform, "Handle");
        if (handle != null)
        {
            Record(handle);
            Record(handle.rectTransform);
            handle.sprite = GetSprite(GemSprite);
            handle.type = Image.Type.Simple;
            handle.preserveAspect = true;
            handle.color = Gold;
            handle.rectTransform.sizeDelta = new Vector2(26, 10);
        }
    }

    private static void StyleCameraBackground(Scene scene)
    {
        Camera cam = scene.GetRootGameObjects()
            .Select(go => go.GetComponent<Camera>())
            .FirstOrDefault(c => c != null);
        if (cam == null) { Debug.LogWarning("[UI Theme] 카메라 없음"); return; }
        Record(cam);
        cam.backgroundColor = CameraBackground;
    }

    // ---- 3-A 도우미 ----

    private static void SetRect(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        if (rt == null) return;
        Record(rt);
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    private static void SetFlexibleWidth(GameObject go, float flexible)
    {
        GetOrAdd<LayoutElement>(go).flexibleWidth = flexible;
    }

    private static void SetDim(Image img)
    {
        if (img == null) return;
        Record(img);
        img.sprite = null;
        img.type = Image.Type.Simple;
        img.color = OverlayDim;
    }

    /// 버튼 라벨: text 가 null 이면 글자는 그대로 두고 크기·색·폰트만.
    private static void SetButtonLabel(Transform button, string text, float size)
    {
        TMP_Text tmp = button.GetComponentInChildren<TMP_Text>(true);
        if (tmp == null) return;
        Record(tmp);
        if (text != null) tmp.text = text;
        tmp.fontSize = size;
        tmp.color = CreamText;
        if (font != null) tmp.font = font;
    }

    private static void SetLabelText(Transform button, string text)
    {
        TMP_Text tmp = button.GetComponentInChildren<TMP_Text>(true);
        if (tmp == null) return;
        Record(tmp);
        tmp.text = text;
    }

    private static void IgnoreLayout(GameObject go)
    {
        GetOrAdd<LayoutElement>(go).ignoreLayout = true;
    }

    private static T GetOrAdd<T>(GameObject go) where T : Component
    {
        T c = go.GetComponent<T>();
        if (c == null) c = AddComp<T>(go);
        else Record(c);
        return c;
    }

    private static void StyleScrollbarsRecorded(Transform root)
    {
        foreach (Scrollbar bar in root.GetComponentsInChildren<Scrollbar>(true))
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
