using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

using TMPro;

/// <summary>
/// 증강 선택용 3장의 카드와 보유 증강 HUD 목록을 표시하는 uGUI 뷰.
/// 선택/리롤 입력은 AugmentManager가 전달한 콜백으로 처리한다.
/// </summary>
public class AugmentSelectUI : MonoBehaviour
{
    // 등급별 텍스트 색상(실버 회색 / 골드 노랑 / 프리즘 보라)이다.
    private static readonly Color _silverColor = new Color(0.75f, 0.78f, 0.80f, 1f);
    private static readonly Color _goldColor = new Color(1f, 0.84f, 0.2f, 1f);
    private static readonly Color _prismaticColor = new Color(0.66f, 0.4f, 1f, 1f);

    [Header("Selection Panel")]
    [SerializeField] private GameObject _selectPanel;
    [SerializeField] private CardView[] _cards;
    [SerializeField] private TextMeshProUGUI _rerollsLabel;

    [Header("HUD")]
    [SerializeField] private RectTransform _hudRoot;
    [SerializeField] private GameObject _hudEntryTemplate;

    private Action<int> _onPick;
    private Action<int> _onReroll;

    /// <summary>
    /// 선택 UI를 열기 전에 카드를 1장 표시하는 뷰 바인딩.
    /// 카드 루트/버튼과 등급·이름·설명 텍스트를 연결한다.
    /// </summary>
    [System.Serializable]
    private class CardView
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private Button _selectButton;
        [SerializeField] private Button _rerollButton;
        [SerializeField] private TextMeshProUGUI _tierText;
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private TextMeshProUGUI _descriptionText;

        /// <summary>
        /// 카드 전체(선택 버튼의 배경) GameObject를 반환한다.
        /// </summary>
        public GameObject Root => _root;

        /// <summary>
        /// 카드 전체 클릭용 선택 버튼을 반환한다.
        /// </summary>
        public Button SelectButton => _selectButton;

        /// <summary>
        /// 카드별 리롤 버튼을 반환한다.
        /// </summary>
        public Button RerollButton => _rerollButton;

        /// <summary>
        /// 등급 텍스트를 반환한다.
        /// </summary>
        public TextMeshProUGUI TierText => _tierText;

        /// <summary>
        /// 이름 텍스트를 반환한다.
        /// </summary>
        public TextMeshProUGUI NameText => _nameText;

        /// <summary>
        /// 설명 텍스트를 반환한다.
        /// </summary>
        public TextMeshProUGUI DescriptionText => _descriptionText;
    }

    /// <summary>
    /// cards(최대 3장, null 포함 가능)와 남은 리롤 수로 선택 패널을 표시하고
    /// 카드 선택 시 onPick(index), 리롤 시 onReroll(index)을 호출한다.
    /// </summary>
    public void Show(IReadOnlyList<AugmentData> cards, int rerollsLeft, Action<int> onPick, Action<int> onReroll)
    {
        _onPick = onPick;
        _onReroll = onReroll;
        _selectPanel.SetActive(true);

        for (int i = 0; i < _cards.Length; i++)
        {
            CardView card = _cards[i];
            AugmentData data = (cards != null && i < cards.Count) ? cards[i] : null;

            card.Root.SetActive(data != null);
            if (data == null)
            {
                continue;
            }

            ApplyCardData(card, data);

            int index = i;
            card.SelectButton.onClick.RemoveAllListeners();
            card.SelectButton.onClick.AddListener(() => _onPick?.Invoke(index));

            card.RerollButton.onClick.RemoveAllListeners();
            card.RerollButton.onClick.AddListener(() => _onReroll?.Invoke(index));
            card.RerollButton.interactable = rerollsLeft > 0;
        }

        UpdateRerollsLabel(rerollsLeft);
    }

    /// <summary>
    /// index의 카드를 data로 교체해 표시하고, 그 카드의 리롤 버튼을 비활성화한다.
    /// 나머지 카드의 리롤 버튼과 라벨은 남은 횟수 rerollsLeft에 맞춘다.
    /// </summary>
    public void ReplaceCard(int index, AugmentData data, int rerollsLeft)
    {
        if (_cards == null || index < 0 || index >= _cards.Length)
        {
            return;
        }

        _cards[index].Root.SetActive(true);
        ApplyCardData(_cards[index], data);
        _cards[index].RerollButton.interactable = false;

        foreach (CardView card in _cards)
        {
            if (card != _cards[index])
            {
                card.RerollButton.interactable = rerollsLeft > 0;
            }
        }

        UpdateRerollsLabel(rerollsLeft);
    }

    /// <summary>
    /// 선택 패널을 숨긴다. HUD 목록은 그대로 유지한다.
    /// </summary>
    public void Hide()
    {
        _selectPanel.SetActive(false);
    }

    /// <summary>
    /// 보유 증강 data를 HUD 목록 하단에 등급 색상의 이름 항목으로 추가한다.
    /// _hudEntryTemplate의 복제본을 _hudRoot 아래에 활성화한다.
    /// </summary>
    public void AddOwned(AugmentData data)
    {
        GameObject entry = Instantiate(_hudEntryTemplate, _hudRoot);
        TextMeshProUGUI text = entry.GetComponent<TextMeshProUGUI>();
        text.text = data.DisplayName;
        text.color = GetTierColor(data.Tier);
        entry.SetActive(true);
    }

    /// <summary>
    /// card에 data의 등급·이름·설명 텍스트와 등급 색상을 반영한다.
    /// </summary>
    private void ApplyCardData(CardView card, AugmentData data)
    {
        card.TierText.text = GetTierName(data.Tier);
        card.TierText.color = GetTierColor(data.Tier);
        card.NameText.text = data.DisplayName;
        card.DescriptionText.text = data.Description;
    }

    /// <summary>
    /// 남은 리롤 수 라벨 텍스트를 rerollsLeft 기준으로 갱신한다.
    /// </summary>
    private void UpdateRerollsLabel(int rerollsLeft)
    {
        _rerollsLabel.text = $"남은 리롤: {rerollsLeft}";
    }

    /// <summary>
    /// tier의 한국어 등급명을 반환한다.
    /// </summary>
    private static string GetTierName(AugmentTier tier)
    {
        switch (tier)
        {
            case AugmentTier.Silver: return "실버";
            case AugmentTier.Gold: return "골드";
            default: return "프리즘";
        }
    }

    /// <summary>
    /// tier에 대응하는 표시 색상을 반환한다.
    /// </summary>
    private static Color GetTierColor(AugmentTier tier)
    {
        switch (tier)
        {
            case AugmentTier.Silver: return _silverColor;
            case AugmentTier.Gold: return _goldColor;
            default: return _prismaticColor;
        }
    }
}
