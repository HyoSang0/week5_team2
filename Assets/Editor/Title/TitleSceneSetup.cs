using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

using UnityEditor;
using UnityEditor.SceneManagement;

using TMPro;

/// <summary>현재 열린 TitleScene에 연습 메뉴를 구성하고 필요한 참조를 저장한다.</summary>
public static class TitleSceneSetup
{
    private const string TITLE_PATH = "Assets/Scenes/TitleScene.unity";
    private const string MAIN_PATH = "Assets/Scenes/MainScene.unity";
    private const string FONT_PATH = "Assets/Fonts/DOSGothic SDF.asset";

    /// <summary>
    /// 열린 타이틀 씬에 메뉴와 연습 관리자를 만들고 본게임 전용 UI를 제거한다.
    /// 고정 씬 경로와 기존 컴포넌트를 사용하며, TitleScene 및 빌드 씬 목록을 저장한다.
    /// </summary>
    [MenuItem("Tools/Title Scene/Apply Approved Setup")]
    public static void Apply()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != TITLE_PATH)
        {
            throw new InvalidOperationException("편집 모드에서 TitleScene을 연 상태로 실행하세요.");
        }
        if (scene.GetRootGameObjects().Any(root => root.name == "TitleMenu"))
        {
            throw new InvalidOperationException("이미 TitleMenu가 구성되어 있습니다.");
        }

        EnemyPool pool = UnityEngine.Object.FindFirstObjectByType<EnemyPool>();
        GroundInitializer ground = UnityEngine.Object.FindFirstObjectByType<GroundInitializer>();
        PlayerHp player = UnityEngine.Object.FindFirstObjectByType<PlayerHp>();
        GameManager manager = UnityEngine.Object.FindFirstObjectByType<GameManager>();
        UiManager hud = UnityEngine.Object.FindFirstObjectByType<UiManager>();
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT_PATH);

        Scene preview = EditorSceneManager.NewPreviewScene();
        try
        {
            GameObject canvasRoot = new GameObject("TitleMenu", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            SceneManager.MoveGameObjectToScene(canvasRoot, preview);
            Canvas canvas = canvasRoot.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;
            CanvasScaler scaler = canvasRoot.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600f, 900f);
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform menu = CreateRect("MenuPanel", canvasRoot.transform,
                Vector2.zero, new Vector2(0.3f, 1f));
            AddImage(menu, UIPalette.SurfaceDark, true);
            RectTransform accent = CreateRect("Accent", menu, new Vector2(0.1f, 0.85f), new Vector2(0.25f, 0.858f));
            AddImage(accent, UIPalette.Accent, false);
            CreateText("Title", menu, "전투 연습장", font, 48f,
                new Vector2(0.1f, 0.7f), new Vector2(0.92f, 0.83f), TextAlignmentOptions.MidlineLeft, UIPalette.TextPrimary);
            CreateText("Subtitle", menu, "가볍게 몸을 풀고\n100초 생존에 도전하세요.", font, 22f,
                new Vector2(0.1f, 0.6f), new Vector2(0.9f, 0.7f), TextAlignmentOptions.MidlineLeft, new Color(0.7f, 0.75f, 0.84f));

            Button start = CreateButton("StartButton", menu, "게임 시작", font, 0.43f, 0.53f);
            Button help = CreateButton("HelpButton", menu, "게임 방법", font, 0.30f, 0.40f);
            Button quit = CreateButton("QuitButton", menu, "게임 종료", font, 0.17f, 0.27f);
            ConfigureNavigation(start, quit, help);
            ConfigureNavigation(help, start, quit);
            ConfigureNavigation(quit, help, start);
            TMP_Text hint = CreateText("MenuHint", menu, "Tab / 패드 시작 버튼 · 메뉴 선택", font, 17f,
                new Vector2(0.08f, 0.04f), new Vector2(0.92f, 0.12f), TextAlignmentOptions.Center, new Color(0.7f, 0.75f, 0.84f));

            RectTransform practiceHeader = CreateRect("PracticeHeader", canvasRoot.transform,
                new Vector2(0.32f, 0.87f), new Vector2(0.98f, 0.98f));
            AddImage(practiceHeader, UIPalette.PanelBackground, false);
            TMP_Text status = CreateText("WaveStatus", practiceHeader, "기본 적 연습 · 4마리부터 시작합니다", font, 25f,
                new Vector2(0.04f, 0.48f), new Vector2(0.96f, 0.98f), TextAlignmentOptions.Center, UIPalette.Accent);
            CreateText("PracticeHint", practiceHeader, "피해 없이 연습 · 기본 적 4마리 / 특수 적 3마리 반복", font, 18f,
                new Vector2(0.02f, 0.02f), new Vector2(0.98f, 0.48f), TextAlignmentOptions.Center, Color.white);
            CreateText("ControlsHint", canvasRoot.transform,
                "WASD 이동   ·   마우스 조준   ·   좌클릭 돌진 공격   ·   우클릭 유지 흡수", font, 19f,
                new Vector2(0.32f, 0.025f), new Vector2(0.98f, 0.085f), TextAlignmentOptions.Center, Color.white);

            RectTransform overlay = CreateRect("HelpOverlay", canvasRoot.transform, Vector2.zero, Vector2.one);
            AddImage(overlay, UIPalette.ModalBackground, true);
            RectTransform card = CreateRect("HelpCard", overlay, new Vector2(0.19f, 0.12f), new Vector2(0.81f, 0.88f));
            AddImage(card, UIPalette.SurfaceDark, true);
            CreateText("HelpTitle", card, "게임 방법", font, 40f,
                new Vector2(0.08f, 0.81f), new Vector2(0.92f, 0.96f), TextAlignmentOptions.MidlineLeft, UIPalette.Accent);
            CreateText("HelpBody", card,
                "이동   WASD / 방향키 / 패드 왼쪽 스틱\n조준   마우스 / 패드 오른쪽 스틱\n돌진 공격   마우스 좌클릭 / Enter / 패드 RT\n흡수   마우스 우클릭 유지 / 패드 LT 유지\n\n기본 적을 모두 없애면 특수 적이 등장합니다.\n폭발형: 제거 후 경고 영역의 땅이 무너집니다.\n가시형: 돌진 공격을 반사하므로 흡수를 활용하세요.\n흡수 불가형: 흡수 대신 돌진 공격을 사용하세요.\n\n연습 중에는 피해를 받지 않고, 떨어지면 복귀합니다.\n본게임에서는 적이 추적합니다. 100초간 생존하세요!",
                font, 23f, new Vector2(0.08f, 0.20f), new Vector2(0.92f, 0.80f), TextAlignmentOptions.TopLeft, Color.white);
            Button close = CreateButton("CloseHelpButton", card, "닫기 · 연습으로 돌아가기", font, 0.045f, 0.15f);
            ConfigureNavigation(close, close, close);

            TitleSceneController title = canvasRoot.AddComponent<TitleSceneController>();
            Bind(title, "_menuPanel", menu);
            Bind(title, "_startButton", start);
            Bind(title, "_helpButton", help);
            Bind(title, "_quitButton", quit);
            Bind(title, "_helpPanel", overlay.gameObject);
            Bind(title, "_closeHelpButton", close);
            Bind(title, "_menuHint", hint);
            overlay.gameObject.SetActive(false);

            GameObject practiceRoot = new GameObject("TitlePractice");
            SceneManager.MoveGameObjectToScene(practiceRoot, preview);
            TitlePracticeController practice = practiceRoot.AddComponent<TitlePracticeController>();
            Bind(practice, "_enemyPool", pool);
            Bind(practice, "_ground", ground);
            Bind(practice, "_player", player.transform);
            Bind(practice, "_statusText", status);
            Vector3[] positions = {
                new Vector3(-5f, 0f, 5f), new Vector3(5f, 0f, 5f),
                new Vector3(-5f, 0f, -5f), new Vector3(5f, 0f, -5f),
                new Vector3(0f, 0f, 9f), new Vector3(9f, 0f, 0f),
                new Vector3(0f, 0f, -9f), new Vector3(-9f, 0f, 0f)
            };
            SerializedObject practiceData = new SerializedObject(practice);
            SerializedProperty points = practiceData.FindProperty("_spawnPoints");
            points.arraySize = positions.Length;
            for (int i = 0; i < positions.Length; i++)
            {
                GameObject point = new GameObject($"PracticeSpawn{i + 1}");
                point.transform.SetParent(practiceRoot.transform, false);
                point.transform.position = positions[i];
                points.GetArrayElementAtIndex(i).objectReferenceValue = point.transform;
            }
            practiceData.ApplyModifiedPropertiesWithoutUndo();

            SceneManager.MoveGameObjectToScene(canvasRoot, scene);
            SceneManager.MoveGameObjectToScene(practiceRoot, scene);
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(preview);
        }

        SetBool(manager, "_isPracticeMode", true);
        SetBool(pool, "_isPracticeMode", true);
        SetBool(player, "_practiceInvincible", true);
        UnityEngine.Object.DestroyImmediate(pool.GetComponent<SpawnManager>());
        UnityEngine.Object.DestroyImmediate(hud.GetComponent<AugmentManager>());

        Transform hudRoot = hud.transform;
        UnityEngine.Object.DestroyImmediate(hud);
        string[] remove = { "TimeText", "ScoreText", "LevelSlider", "SliderGroup", "UIAugmentHudView", "UIAugmentSelectionView", "GameOverGroup" };
        foreach (string name in remove)
        {
            Transform child = hudRoot.Find(name);
            if (child != null)
            {
                UnityEngine.Object.DestroyImmediate(child.gameObject);
            }
        }
        foreach (string field in new[] { "timeText", "gameOverText", "scoreText", "_gameOverGroup", "_gameOverSelection", "infiniteModeButton" })
        {
            Bind(manager, field, null);
        }
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name.StartsWith("---", StringComparison.Ordinal) || root.name == "Ground_Temp")
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        Camera camera = Camera.main;
        camera.rect = new Rect(0.3f, 0f, 0.7f, 1f);
        camera.orthographic = true;
        camera.orthographicSize = 18f;
        EditorUtility.SetDirty(camera);

        List<EditorBuildSettingsScene> buildScenes = new List<EditorBuildSettingsScene>
        {
            new EditorBuildSettingsScene(TITLE_PATH, true),
            new EditorBuildSettingsScene(MAIN_PATH, true)
        };
        buildScenes.AddRange(EditorBuildSettings.scenes.Where(s => s.path != TITLE_PATH && s.path != MAIN_PATH));
        EditorBuildSettings.scenes = buildScenes.ToArray();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("TitleScene 메뉴와 반복 연습 구성을 저장했습니다.");
    }

    /// <summary>
    /// 편집 중인 타이틀의 게임 방법 패널을 미리 표시한다.
    /// 현재 씬의 TitleMenu를 사용하며 런타임 버튼이나 일시정지 상태는 실행하지 않는다.
    /// </summary>
    [MenuItem("Tools/Title Scene/Preview Help")]
    public static void PreviewHelp()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || SceneManager.GetActiveScene().path != TITLE_PATH)
        {
            return;
        }
        UnityEngine.Object.FindFirstObjectByType<TitleSceneController>().transform.Find("HelpOverlay").gameObject.SetActive(true);
        Canvas.ForceUpdateCanvases();
        UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
    }

    /// <summary>
    /// 타이틀 미리보기 패널을 닫고 기본 메뉴 상태를 저장한다.
    /// 현재 TitleMenu를 사용하며 연습 안내 문구와 씬 저장 상태를 갱신한다.
    /// </summary>
    [MenuItem("Tools/Title Scene/Finish Layout")]
    public static void FinishLayout()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != TITLE_PATH)
        {
            return;
        }
        Transform root = UnityEngine.Object.FindFirstObjectByType<TitleSceneController>().transform;
        root.Find("HelpOverlay").gameObject.SetActive(false);
        root.Find("PracticeHeader/PracticeHint").GetComponent<TMP_Text>().text = "피해 없이 연습 · 기본 적 4마리 / 특수 적 3마리 반복";
        Canvas.ForceUpdateCanvases();
        UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    /// <summary>
    /// 부모 아래 정규화 앵커 범위에 UI 사각형을 만든다.
    /// name, parent, min, max를 사용해 여백 없는 RectTransform을 반환한다.
    /// </summary>
    private static RectTransform CreateRect(string name, Transform parent, Vector2 min, Vector2 max)
    {
        RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return rect;
    }

    /// <summary>
    /// UI 사각형에 지정 색상의 이미지를 추가한다.
    /// rect, color, raycast를 사용하며 생성한 Image를 반환한다.
    /// </summary>
    private static Image AddImage(RectTransform rect, Color color, bool raycast)
    {
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = raycast;
        return image;
    }

    /// <summary>
    /// 지정 앵커에 한국어 폰트를 사용하는 텍스트를 생성한다.
    /// 문자열, 폰트, 크기, 정렬, 색상을 적용하고 생성된 TMP_Text를 반환한다.
    /// </summary>
    private static TMP_Text CreateText(string name, Transform parent, string text, TMP_FontAsset font, float size,
        Vector2 min, Vector2 max, TextAlignmentOptions alignment, Color color)
    {
        TMP_Text label = CreateRect(name, parent, min, max).gameObject.AddComponent<TextMeshProUGUI>();
        label.font = font;
        label.text = text;
        label.fontSize = size;
        label.enableAutoSizing = true;
        label.fontSizeMin = size * 0.75f;
        label.fontSizeMax = size;
        label.alignment = alignment;
        label.color = color;
        label.raycastTarget = false;
        return label;
    }

    /// <summary>
    /// 지정 세로 범위에 공용 팔레트의 메뉴 버튼을 생성한다.
    /// 이름과 라벨, 부모, 폰트, 앵커를 사용하며 구성된 Button을 반환한다.
    /// </summary>
    private static Button CreateButton(string name, Transform parent, string label, TMP_FontAsset font, float bottom, float top)
    {
        RectTransform rect = CreateRect(name, parent, new Vector2(0.1f, bottom), new Vector2(0.9f, top));
        Image image = AddImage(rect, Color.white, true);
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.colors = UIPalette.CreateButtonColors();
        CreateText("Label", rect, label, font, 29f, new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.95f), TextAlignmentOptions.Center, Color.white);
        rect.gameObject.AddComponent<UISelectionHighlight>();
        return button;
    }

    /// <summary>
    /// 버튼 간 위아래 탐색을 명시적으로 연결한다.
    /// button, up, down을 사용하여 다른 패널로 선택이 이동하지 않도록 설정한다.
    /// </summary>
    private static void ConfigureNavigation(Button button, Button up, Button down)
    {
        button.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = up, selectOnDown = down };
    }

    /// <summary>
    /// 대상 컴포넌트의 직렬화된 오브젝트 참조를 설정한다.
    /// target, field, value를 사용하며 수정된 참조를 즉시 적용한다.
    /// </summary>
    private static void Bind(UnityEngine.Object target, string field, UnityEngine.Object value)
    {
        SerializedObject data = new SerializedObject(target);
        data.FindProperty(field).objectReferenceValue = value;
        data.ApplyModifiedPropertiesWithoutUndo();
    }

    /// <summary>
    /// 대상 컴포넌트의 직렬화된 bool 설정값을 변경한다.
    /// target, field, value를 사용하며 수정한 연습 설정을 즉시 적용한다.
    /// </summary>
    private static void SetBool(UnityEngine.Object target, string field, bool value)
    {
        SerializedObject data = new SerializedObject(target);
        data.FindProperty(field).boolValue = value;
        data.ApplyModifiedPropertiesWithoutUndo();
    }
}
