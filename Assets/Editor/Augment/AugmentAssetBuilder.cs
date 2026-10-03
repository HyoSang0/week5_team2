using System.Collections.Generic;
using System.IO;

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

using TMPro;

using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// 증강 데이터 에셋 18종, AugmentDatabase, AugmentSystem 프리팹을 생성/갱신하는 에디터 유틸리티.
/// unity cmd eval 또는 메뉴로 BuildAll(), BuildPrefab()을 실행한다.
/// </summary>
public static class AugmentAssetBuilder
{
    private const string DATA_FOLDER = "Assets/Data/Augments";
    private const string PREFAB_FOLDER = "Assets/Prefabs/Augment";
    private const string DATABASE_PATH = DATA_FOLDER + "/AugmentDatabase.asset";
    private const string PREFAB_PATH = PREFAB_FOLDER + "/AugmentSystem.prefab";
    private const string CARD_PREFAB_PATH = PREFAB_FOLDER + "/AugmentCard.prefab";
    private const string FONT_ASSET_PATH = "Assets/Fonts/DOSGothic SDF.asset";

    private const float PANEL_DIM_ALPHA = 0.72f;

    /// <summary>
    /// 정의된 증강 데이터 18종, AugmentDatabase, 효과 SO 5종을 Assets/Data/Augments/에 생성한다.
    /// 이미 존재하는 에셋은 새로 만들지 않고 직렬화 값만 갱신한다.
    /// </summary>
    [MenuItem("Tools/Augment/Build Data Assets")]
    public static void BuildAll()
    {
        EnsureFolder("Assets", "Data");
        EnsureFolder(DATA_FOLDER, null);

        List<AugmentData> all = new List<AugmentData>();
        foreach (AugmentDefinition definition in GetDefinitions())
        {
            all.Add(BuildAugmentData(definition));
        }

        AugmentDatabase database = LoadOrCreate<AugmentDatabase>(DATABASE_PATH);
        SerializedObject databaseSO = new SerializedObject(database);
        SerializedProperty augmentsProperty = databaseSO.FindProperty("_augments");
        augmentsProperty.arraySize = all.Count;
        for (int i = 0; i < all.Count; i++)
        {
            augmentsProperty.GetArrayElementAtIndex(i).objectReferenceValue = all[i];
        }

        databaseSO.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(database);

        BuildEffects();

        AssetDatabase.SaveAssets();

        Debug.Log($"[AugmentAssetBuilder] {all.Count}개 증강 에셋과 DB를 갱신했습니다: {DATABASE_PATH}");
    }

    /// <summary>
    /// AugmentSystem 프리팹(루트: AugmentManager+PlayerStats, 자식: 선택 UI Canvas)을
    /// preview 씬에서 조립해 Assets/Prefabs/Augment/AugmentSystem.prefab으로 저장한다.
    /// 선택 카드용 AugmentCard 프리팹이 없으면 먼저 생성한다. 열려 있는 씬은 변경하지 않는다.
    /// </summary>
    [MenuItem("Tools/Augment/Build System Prefab")]
    public static void BuildPrefab()
    {
        EnsureFolder("Assets", "Prefabs");
        EnsureFolder(PREFAB_FOLDER, null);

        AugmentDatabase database = AssetDatabase.LoadAssetAtPath<AugmentDatabase>(DATABASE_PATH);

        Scene previewScene = EditorSceneManager.NewPreviewScene();

        GameObject root = new GameObject("AugmentSystem");
        SceneManager.MoveGameObjectToScene(root, previewScene);

        root.AddComponent<PlayerStats>();
        AugmentManager manager = root.AddComponent<AugmentManager>();

        RectTransform canvasRect = CreateRect("Canvas", root.transform);
        Canvas canvas = canvasRect.gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasRect.gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasRect.gameObject.AddComponent<GraphicRaycaster>();

        AugmentSelectionView selectionView = BuildSelectionView(canvas, EnsureCardPrefab());
        AugmentHudView hudView = BuildHudView(canvas.transform);

        SerializedObject managerSO = new SerializedObject(manager);
        managerSO.FindProperty("_database").objectReferenceValue = database;
        managerSO.FindProperty("_selectionView").objectReferenceValue = selectionView;
        managerSO.FindProperty("_hudView").objectReferenceValue = hudView;
        managerSO.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(root, PREFAB_PATH);
        EditorSceneManager.ClosePreviewScene(previewScene);

        AssetDatabase.SaveAssets();
        Debug.Log($"[AugmentAssetBuilder] 프리팹을 저장했습니다: {PREFAB_PATH}");
    }

    /// <summary>
    /// definition 하나의 값을 AugmentData 에셋에 반영해 path에 저장하고 반환한다.
    /// 기존 에셋이 있으면 값만 갱신하고, 없으면 새로 생성한다.
    /// </summary>
    private static AugmentData BuildAugmentData(AugmentDefinition definition)
    {
        string path = $"{DATA_FOLDER}/{definition.Id}.asset";
        AugmentData data = LoadOrCreate<AugmentData>(path);

        SerializedObject so = new SerializedObject(data);
        so.FindProperty("_id").stringValue = definition.Id;
        so.FindProperty("_displayName").stringValue = definition.DisplayName;
        so.FindProperty("_description").stringValue = definition.Description;
        so.FindProperty("_tier").enumValueIndex = (int)definition.Tier;

        SetStringArray(so.FindProperty("_conflictTags"), definition.ConflictTags);
        SetModifiers(so.FindProperty("_modifiers"), definition.Modifiers);

        // 스탯형으로 바뀌는 에셋에 이전 효과형 참조가 남지 않도록 효과 참조를 먼저 비운다.
        so.FindProperty("_effect").objectReferenceValue = null;

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(data);
        return data;
    }

    /// <summary>
    /// 18개 증강의 id/표시명/설명/등급/충돌태그/수정자 정의를 반환한다.
    /// 효과형 5개(feast, chain_heal, energy_cycle, chain_explosion, bomb_collector)는 modifiers만 비운다.
    /// </summary>
    private static AugmentDefinition[] GetDefinitions()
    {
        return new[]
        {
            new AugmentDefinition
            {
                Id = "light_feet", DisplayName = "가벼운 발", Description = "이동 속도가 15% 증가합니다.",
                Tier = AugmentTier.Silver,
                Modifiers = new[] { new StatModifier(StatType.MoveSpeed, 15f, 0f) },
            },
            new AugmentDefinition
            {
                Id = "sturdy_body", DisplayName = "튼튼한 몸", Description = "최대 체력이 1 증가합니다. 체력 관련 증강과 충돌합니다.",
                Tier = AugmentTier.Silver, ConflictTags = new[] { "MaxHp" },
                Modifiers = new[] { new StatModifier(StatType.MaxHp, 0f, 1f) },
            },
            new AugmentDefinition
            {
                Id = "long_breath", DisplayName = "빠른 확장", Description = "흡수 영역 확장 속도가 25% 증가합니다.",
                Tier = AugmentTier.Silver,
                Modifiers = new[] { new StatModifier(StatType.AbsorbGrowSpeed, 25f, 0f) },
            },
            new AugmentDefinition
            {
                Id = "wide_hands", DisplayName = "넓은 손", Description = "흡수 반경이 15% 증가합니다.",
                Tier = AugmentTier.Silver,
                Modifiers = new[] { new StatModifier(StatType.AbsorbRadius, 15f, 0f) },
            },
            new AugmentDefinition
            {
                Id = "warm_up", DisplayName = "예열", Description = "돌진 쿨다운이 20% 감소합니다.",
                Tier = AugmentTier.Silver,
                Modifiers = new[] { new StatModifier(StatType.RushCooldown, -20f, 0f) },
            },
            new AugmentDefinition
            {
                Id = "heavy_fist", DisplayName = "무거운 주먹", Description = "적에게 가하는 넉백 힘이 20% 증가합니다.",
                Tier = AugmentTier.Silver,
                Modifiers = new[] { new StatModifier(StatType.KnockbackForce, 20f, 0f) },
            },
            new AugmentDefinition
            {
                Id = "fast_recovery", DisplayName = "긴 돌진", Description = "돌진 지속 시간이 30% 증가합니다.",
                Tier = AugmentTier.Gold,
                Modifiers = new[] { new StatModifier(StatType.RushDuration, 30f, 0f) },
            },
            new AugmentDefinition
            {
                Id = "efficiency", DisplayName = "재정비", Description = "돌진 쿨다운이 30% 감소합니다.",
                Tier = AugmentTier.Gold,
                Modifiers = new[] { new StatModifier(StatType.RushCooldown, -30f, 0f) },
            },
            new AugmentDefinition
            {
                Id = "invincible_rush", DisplayName = "무적 돌진", Description = "돌진 중 0.5초 동안 무적이 됩니다.",
                Tier = AugmentTier.Gold,
                Modifiers = new[] { new StatModifier(StatType.RushInvincible, 0f, 0.5f) },
            },
            new AugmentDefinition
            {
                Id = "domino", DisplayName = "도미노", Description = "흡수 시 연쇄 피해가 100% 증가합니다.",
                Tier = AugmentTier.Gold,
                Modifiers = new[] { new StatModifier(StatType.ChainDamage, 100f, 0f) },
            },
            new AugmentDefinition
            {
                Id = "score_hunter", DisplayName = "점수 사냥꾼", Description = "획득 점수가 50% 증가합니다.",
                Tier = AugmentTier.Gold,
                Modifiers = new[] { new StatModifier(StatType.ScoreMultiplier, 50f, 0f) },
            },
            new AugmentDefinition
            {
                Id = "feast", DisplayName = "포식", Description = "적을 흡수할 때마다 돌진 쿨다운이 0.3초 감소합니다.",
                Tier = AugmentTier.Gold,
            },
            new AugmentDefinition
            {
                Id = "chain_heal", DisplayName = "연쇄 회복", Description = "한 번의 돌진 중 5번째 처치 시 HP를 1 회복합니다.",
                Tier = AugmentTier.Gold,
            },
            new AugmentDefinition
            {
                Id = "black_hole", DisplayName = "블랙홀", Description = "흡수 반경이 100% 증가하지만 이동 속도가 20% 감소합니다.",
                Tier = AugmentTier.Prismatic,
                Modifiers = new[]
                {
                    new StatModifier(StatType.AbsorbRadius, 100f, 0f),
                    new StatModifier(StatType.MoveSpeed, -20f, 0f),
                },
            },
            new AugmentDefinition
            {
                Id = "glass_cannon", DisplayName = "유리 대포", Description = "돌진 속도가 50% 증가하고 돌진 쿨다운이 50% 감소하지만 최대 체력이 100% 감소합니다. 체력 관련 증강과 충돌합니다.",
                Tier = AugmentTier.Prismatic, ConflictTags = new[] { "MaxHp" },
                Modifiers = new[]
                {
                    new StatModifier(StatType.RushSpeed, 50f, 0f),
                    new StatModifier(StatType.RushCooldown, -50f, 0f),
                    new StatModifier(StatType.MaxHp, -100f, 0f),
                },
            },
            new AugmentDefinition
            {
                Id = "energy_cycle", DisplayName = "에너지 순환", Description = "적을 처치할 때마다 돌진 쿨다운이 0.2초 감소하며, 돌진 중 처치도 다음 쿨다운에 반영됩니다.",
                Tier = AugmentTier.Prismatic,
            },
            new AugmentDefinition
            {
                Id = "chain_explosion", DisplayName = "연쇄 폭발", Description = "적을 처치하면 반지름 2m 안의 다른 적도 함께 처치하며, 연쇄 처치는 재귀 폭발을 일으키지 않습니다.",
                Tier = AugmentTier.Prismatic,
            },
            new AugmentDefinition
            {
                Id = "bomb_collector", DisplayName = "폭탄 수집가", Description = "폭탄형 적을 흡수하면 땅 붕괴 대신 반지름 3m 안의 주변 적을 처치하며, 연쇄 처치는 중첩 발동하지 않습니다.",
                Tier = AugmentTier.Prismatic,
            },
        };
    }

    /// <summary>
    /// Canvas 아래에 선택 패널(리롤 라벨, 카드 컨테이너)을 만들고
    /// AugmentSelectionView 컴포넌트에 직렬화 참조를 연결해 반환한다.
    /// 카드는 cardPrefab 프리팹로 생성된다.
    /// </summary>
    private static AugmentSelectionView BuildSelectionView(Canvas canvas, AugmentCardView cardPrefab)
    {
        RectTransform panel = CreateRect("SelectPanel", canvas.transform);
        Stretch(panel);
        Image panelBackground = panel.gameObject.AddComponent<Image>();
        panelBackground.color = new Color(0f, 0f, 0f, PANEL_DIM_ALPHA);
        panel.gameObject.layer = LayerMask.NameToLayer("UI");

        AugmentSelectionView view = panel.gameObject.AddComponent<AugmentSelectionView>();

        TextMeshProUGUI rerollsLabel = CreateText("RerollsLabel", panel,
            "남은 리롤: 3", 30f, Vector2.zero, new Vector2(600f, 50f), TextAlignmentOptions.Center);

        // 씬에서 조정된 값: 화면 하단 중앙 기준 y 100 위치에 고정한다.
        RectTransform rerollsRect = (RectTransform)rerollsLabel.transform;
        rerollsRect.anchorMin = new Vector2(0.5f, 0f);
        rerollsRect.anchorMax = new Vector2(0.5f, 0f);
        rerollsRect.anchoredPosition = new Vector2(0f, 100f);

        RectTransform cardsRow = CreateRect("CardsRow", panel);
        Center(cardsRow, Vector2.zero, Vector2.zero);
        cardsRow.localScale = new Vector3(0.75f, 0.75f, 1f);

        HorizontalLayoutGroup cardLayout = cardsRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        cardLayout.childAlignment = TextAnchor.MiddleCenter;
        cardLayout.childControlWidth = false;
        cardLayout.childControlHeight = false;
        cardLayout.childForceExpandWidth = false;
        cardLayout.childForceExpandHeight = false;
        cardLayout.spacing = 40f;

        UIDefaultSelection defaultSelection = panel.gameObject.AddComponent<UIDefaultSelection>();

        SerializedObject uiSO = new SerializedObject(view);
        uiSO.FindProperty("_selectPanel").objectReferenceValue = panel.gameObject;
        uiSO.FindProperty("_cardPrefab").objectReferenceValue = cardPrefab;
        uiSO.FindProperty("_cardContainer").objectReferenceValue = cardsRow;
        uiSO.FindProperty("_rerollsLabel").objectReferenceValue = rerollsLabel;
        uiSO.FindProperty("_defaultSelection").objectReferenceValue = defaultSelection;

        uiSO.ApplyModifiedPropertiesWithoutUndo();

        panel.gameObject.SetActive(false);
        return view;
    }

    /// <summary>
    /// CARD_PREFAB_PATH의 카드 프리팹 에셋을 읽어 반환한다. 없으면 BuildCardPrefab()으로 먼저 생성한다.
    /// </summary>
    private static AugmentCardView EnsureCardPrefab()
    {
        AugmentCardView cardPrefab = AssetDatabase.LoadAssetAtPath<AugmentCardView>(CARD_PREFAB_PATH);
        if (cardPrefab == null)
        {
            BuildCardPrefab();
            cardPrefab = AssetDatabase.LoadAssetAtPath<AugmentCardView>(CARD_PREFAB_PATH);
        }

        return cardPrefab;
    }

    /// <summary>
    /// 증강 선택 카드 한 장(AugmentCardView + 선택/리롤 버튼 + 등급·이름·설명 텍스트)을
    /// preview 씬에서 조립해 Assets/Prefabs/Augment/AugmentCard.prefab으로 저장한다.
    /// </summary>
    public static void BuildCardPrefab()
    {
        EnsureFolder("Assets", "Prefabs");
        EnsureFolder(PREFAB_FOLDER, null);

        Scene previewScene = EditorSceneManager.NewPreviewScene();

        GameObject card = new GameObject("AugmentCard", typeof(RectTransform));
        card.layer = LayerMask.NameToLayer("UI");
        SceneManager.MoveGameObjectToScene(card, previewScene);
        RectTransform cardRect = (RectTransform)card.transform;
        cardRect.sizeDelta = new Vector2(420f, 560f);

        Image cardImage = card.AddComponent<Image>();
        cardImage.color = new Color(0.13f, 0.14f, 0.18f, 1f);

        Button selectButton = card.AddComponent<Button>();
        selectButton.targetGraphic = cardImage;

        TextMeshProUGUI tierText = CreateText("TierText", cardRect, "등급", 40f,
            new Vector2(0f, 230f), new Vector2(420f, 60f), TextAlignmentOptions.Center);
        TextMeshProUGUI nameText = CreateText("NameText", cardRect, "이름", 36f,
            new Vector2(0f, 150f), new Vector2(380f, 56f), TextAlignmentOptions.Center);
        TextMeshProUGUI descText = CreateText("DescText", cardRect, "설명", 26f,
            new Vector2(0f, -40f), new Vector2(380f, 320f), TextAlignmentOptions.TopLeft);
        descText.margin = new Vector4(10f, 30f, 10f, 10f);

        RectTransform rerollRect = CreateRect("RerollButton", cardRect);
        Center(rerollRect, new Vector2(0f, -240f), new Vector2(180f, 64f));
        Image rerollImage = rerollRect.gameObject.AddComponent<Image>();
        rerollImage.color = new Color(0.25f, 0.35f, 0.6f, 1f);
        Button rerollButton = rerollRect.gameObject.AddComponent<Button>();
        CreateText("Label", rerollRect, "리롤", 28f, Vector2.zero, new Vector2(180f, 64f), TextAlignmentOptions.Center);

        AugmentCardView view = card.AddComponent<AugmentCardView>();
        SerializedObject viewSO = new SerializedObject(view);
        viewSO.FindProperty("_selectButton").objectReferenceValue = selectButton;
        viewSO.FindProperty("_rerollButton").objectReferenceValue = rerollButton;
        viewSO.FindProperty("_tierText").objectReferenceValue = tierText;
        viewSO.FindProperty("_nameText").objectReferenceValue = nameText;
        viewSO.FindProperty("_descriptionText").objectReferenceValue = descText;
        viewSO.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(card, CARD_PREFAB_PATH);
        EditorSceneManager.ClosePreviewScene(previewScene);

        AssetDatabase.SaveAssets();
        Debug.Log($"[AugmentAssetBuilder] 카드 프리팹을 저장했습니다: {CARD_PREFAB_PATH}");
    }

    /// <summary>
    /// 화면 좌상단 HUD 목록(보유 증강 이름 세로 목록)과 비활성화된 항목 템플릿을 만들고
    /// AugmentHudView 컴포넌트에 직렬화 참조를 연결해 반환한다.
    /// </summary>
    private static AugmentHudView BuildHudView(Transform canvasTransform)
    {
        RectTransform hud = CreateRect("HUD", canvasTransform);
        hud.anchorMin = new Vector2(0f, 1f);
        hud.anchorMax = new Vector2(0f, 1f);
        hud.pivot = new Vector2(0f, 1f);
        hud.anchoredPosition = new Vector2(20f, -90f);
        hud.sizeDelta = new Vector2(200f, 600f);

        // 씬에서 조정된 값: 반투명 배경 Image와 좌측 여백 20의 세로 목록 레이아웃을 사용한다.
        Image hudBackground = hud.gameObject.AddComponent<Image>();
        hudBackground.color = new Color(0.1f, 0.1f, 0.1f, 50f / 255f);

        VerticalLayoutGroup layout = hud.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.spacing = 4f;
        layout.padding = new RectOffset(20, 0, 0, 0);

        RectTransform template = CreateRect("OwnedEntryTemplate", hud);
        template.anchorMin = new Vector2(0f, 1f);
        template.anchorMax = new Vector2(0f, 1f);
        template.pivot = new Vector2(0f, 1f);
        template.anchoredPosition = new Vector2(20f, 0f);
        template.sizeDelta = new Vector2(0f, 25f);
        TextMeshProUGUI text = template.gameObject.AddComponent<TextMeshProUGUI>();
        text.font = GetKoreanFontAsset();
        text.fontSize = 24f;
        text.alignment = TextAlignmentOptions.Left;
        text.color = Color.white;
        template.gameObject.SetActive(false);

        AugmentHudView view = hud.gameObject.AddComponent<AugmentHudView>();
        SerializedObject viewSO = new SerializedObject(view);
        viewSO.FindProperty("_hudRoot").objectReferenceValue = hud;
        viewSO.FindProperty("_hudEntryTemplate").objectReferenceValue = template.gameObject;
        viewSO.ApplyModifiedPropertiesWithoutUndo();

        return view;
    }

    /// <summary>
    /// 효과 SO 5개를 Assets/Data/Augments/Effects/에 생성 또는 갱신하고 해당 AugmentData의 _effect에 연결한다.
    /// 스탯형 에셋과 AugmentData의 다른 값은 변경하지 않는다.
    /// </summary>
    private static void BuildEffects()
    {
        string effectFolder = DATA_FOLDER + "/Effects";
        EnsureFolder(DATA_FOLDER, "Effects");

        FeastEffect feast = EnsureEffect<FeastEffect>("FeastEffect", effectFolder);
        SetEffectFloat(feast, "_cooldownReductionPerAbsorb", 0.3f);
        LinkEffect("feast", feast, "적을 흡수할 때마다 돌진 쿨다운이 0.3초 감소합니다.");

        LinkEffect("chain_heal", EnsureEffect<ChainHealEffect>("ChainHealEffect", effectFolder),
            "한 번의 돌진 중 5번째 처치 시 HP를 1 회복합니다.");

        EnergyCycleEffect energyCycle = EnsureEffect<EnergyCycleEffect>("EnergyCycleEffect", effectFolder);
        SetEffectFloat(energyCycle, "_cooldownReductionPerKill", 0.2f);
        LinkEffect("energy_cycle", energyCycle, "적을 처치할 때마다 돌진 쿨다운이 0.2초 감소하며, 돌진 중 처치도 다음 쿨다운에 반영됩니다.");

        LinkEffect("chain_explosion", EnsureEffect<ChainExplosionEffect>("ChainExplosionEffect", effectFolder),
            "적을 처치하면 반지름 2m 안의 다른 적도 함께 처치하며, 연쇄 처치는 재귀 폭발을 일으키지 않습니다.");
        LinkEffect("bomb_collector", EnsureEffect<BombCollectorEffect>("BombCollectorEffect", effectFolder),
            "폭탄형 적을 흡수하면 땅 붕괴 대신 반지름 3m 안의 주변 적을 처치하며, 연쇄 처치는 중첩 발동하지 않습니다.");
    }

    /// <summary>
    /// folder에 name 에셋이 있으면 그대로 반환하고 없으면 T 인스턴스를 만들어 저장한다.
    /// </summary>
    private static T EnsureEffect<T>(string name, string folder) where T : AugmentEffect
    {
        string path = $"{folder}/{name}.asset";
        T effect = AssetDatabase.LoadAssetAtPath<T>(path);
        if (effect == null)
        {
            effect = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(effect, path);
        }

        return effect;
    }

    /// <summary>
    /// effect의 SerializedObject에서 propName float 필드에 value를 써서 현재 필드명을 확정하고,
    /// 클래스에 없는 stale 필드가 직렬화에서 제거되도록 반영한다.
    /// </summary>
    private static void SetEffectFloat(AugmentEffect effect, string propName, float value)
    {
        SerializedObject so = new SerializedObject(effect);
        SerializedProperty prop = so.FindProperty(propName);
        if (prop == null)
        {
            Debug.LogError($"[AugmentAssetBuilder] {effect.name}에 {propName} 필드가 없습니다.");
            return;
        }

        prop.floatValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(effect);
    }

    /// <summary>
    /// id의 AugmentData 에셋의 _effect 필드에 effect를 연결하고 _description을 description으로 갱신한다.
    /// private 직렬화 필드에 직접 쓴다.
    /// </summary>
    private static void LinkEffect(string id, AugmentEffect effect, string description)
    {
        AugmentData data = AssetDatabase.LoadAssetAtPath<AugmentData>($"{DATA_FOLDER}/{id}.asset");
        if (data == null)
        {
            Debug.LogError($"[AugmentAssetBuilder] {id} 에셋을 찾을 수 없어 효과를 연결하지 못했습니다.");
            return;
        }

        SerializedObject so = new SerializedObject(data);
        so.FindProperty("_description").stringValue = description;
        so.FindProperty("_effect").objectReferenceValue = effect;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(data);
    }

    /// <summary>
    /// name으로 RectTransform GameObject를 만들어 parent에 자식으로 붙이고 UI 레이어로 설정해 반환한다.
    /// </summary>
    private static RectTransform CreateRect(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        return rect;
    }

    /// <summary>
    /// rect를 부모 전체에 가득 차도록(stretch full) 확장한다.
    /// </summary>
    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    /// <summary>
    /// rect를 부모 중앙 기준 anchoredPosition positon, 크기 size로 배치한다.
    /// </summary>
    private static void Center(RectTransform rect, Vector2 positon, Vector2 size)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = positon;
        rect.sizeDelta = size;
    }

    /// <summary>
    /// parent 아래 center 기준 위치/크기의 TextMeshProUGUI GameObject를 만들어 반환한다.
    /// 한글 폰트(DOSGothic SDF)를 사용한다.
    /// </summary>
    private static TextMeshProUGUI CreateText(string name, RectTransform parent, string value, float size,
        Vector2 anchoredPosition, Vector2 scale, TextAlignmentOptions alignment)
    {
        RectTransform rect = CreateRect(name, parent);
        Center(rect, anchoredPosition, scale);

        TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.font = GetKoreanFontAsset();
        text.text = value;
        text.fontSize = size;
        text.alignment = alignment;
        text.color = Color.white;
        text.richText = true;
        return text;
    }

    /// <summary>
    /// FONT_ASSET_PATH의 한글 TMP 폰트 에셋을 읽어 반환한다.
    /// 에셋이 없으면 TMP_Settings.defaultFontAsset으로 폴백한다.
    /// </summary>
    private static TMP_FontAsset GetKoreanFontAsset()
    {
        TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT_ASSET_PATH);
        return fontAsset != null ? fontAsset : TMP_Settings.defaultFontAsset;
    }

    /// <summary>
    /// path에 에셋이 있으면 그대로 반환하고, 없으면 새로 만들어 저장한다.
    /// </summary>
    private static T LoadOrCreate<T>(string path) where T : ScriptableObject
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
        }

        return asset;
    }

    /// <summary>
    /// prop(문자열 배열)의 크기를 values에 맞추고 각 요소를 복사한다.
    /// </summary>
    private static void SetStringArray(SerializedProperty prop, string[] values)
    {
        int length = values != null ? values.Length : 0;
        prop.arraySize = length;
        for (int i = 0; i < length; i++)
        {
            prop.GetArrayElementAtIndex(i).stringValue = values[i];
        }
    }

    /// <summary>
    /// prop(StatModifier 배열)의 크기를 modifiers에 맞추고 각 요소의 stat/percent/flat 값을 복사한다.
    /// StatModifier의 private 직렬화 필드를 직접 갱신한다.
    /// </summary>
    private static void SetModifiers(SerializedProperty prop, StatModifier[] modifiers)
    {
        int length = modifiers != null ? modifiers.Length : 0;
        prop.arraySize = length;
        for (int i = 0; i < length; i++)
        {
            SerializedProperty element = prop.GetArrayElementAtIndex(i);
            element.FindPropertyRelative("_stat").enumValueIndex = (int)modifiers[i].Stat;
            element.FindPropertyRelative("_percent").floatValue = modifiers[i].Percent;
            element.FindPropertyRelative("_flat").floatValue = modifiers[i].Flat;
        }
    }

    /// <summary>
    /// parentPath 하위에 folderName 폴더가 없으면 만들고 유틸리티 자산 폴더로 보장한다.
    /// folderName이 null이면 parentPath 자체를 보장한다.
    /// </summary>
    private static void EnsureFolder(string parentPath, string folderName)
    {
        string target = folderName != null ? Path.Combine(parentPath, folderName) : parentPath;
        if (!AssetDatabase.IsValidFolder(target))
        {
            if (folderName != null)
            {
                AssetDatabase.CreateFolder(parentPath, folderName);
            }
            else
            {
                Directory.CreateDirectory(target);
                AssetDatabase.Refresh();
            }
        }
    }

    /// <summary>
    /// 증강 데이터 에셋 하나를 정의하는 에디터 전용 자료형.
    /// </summary>
    private struct AugmentDefinition
    {
        public string Id;
        public string DisplayName;
        public string Description;
        public AugmentTier Tier;
        public string[] ConflictTags;
        public StatModifier[] Modifiers;
    }
}
