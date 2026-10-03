using System.Collections.Generic;

using UnityEngine;

using TMPro;

/// <summary>
/// 증강 선택용 3장의 카드와 남은 리롤 수를 표시하는 선택 패널 뷰.
/// 카드 클릭은 AugmentCardView를 거쳐 바인딩된 IAugmentSelectionHandler에게만 전달하고 판단은 위임한다.
/// </summary>
public class AugmentSelectionView : MonoBehaviour, IAugmentCardHandler
{
    // AugmentManager의 추첨 카드 수와 동일한 값이다.
    private const int CARD_COUNT = 3;

    [Header("Selection Panel")]
    [SerializeField] private GameObject _selectPanel;
    [SerializeField] private AugmentCardView _cardPrefab;
    [SerializeField] private Transform _cardContainer;
    [SerializeField] private TextMeshProUGUI _rerollsLabel;

    private readonly List<AugmentCardView> _cards = new List<AugmentCardView>();
    private IAugmentSelectionHandler _handler;

    void Awake()
    {
        for (int i = 0; i < CARD_COUNT; i++)
        {
            AugmentCardView card = Instantiate(_cardPrefab, _cardContainer);
            card.Bind(i, this);
            _cards.Add(card);
        }
    }

    /// <summary>
    /// 카드 선택/리롤 입력을 전달받을 handler를 바인딩한다.
    /// 반환값은 없고 이후 카드 클릭이 handler를 통해 위임된다.
    /// </summary>
    public void Bind(IAugmentSelectionHandler handler)
    {
        _handler = handler;
    }

    /// <summary>
    /// cards(최대 3장, null 포함 가능)와 남은 리롤 수 rerollsLeft로 선택 패널을 표시한다.
    /// 카드 클릭 시 handler의 HandlePickClicked, 리롤 시 HandleRerollClicked이 호출된다.
    /// </summary>
    public void ShowCards(IReadOnlyList<AugmentData> cards, int rerollsLeft)
    {
        _selectPanel.SetActive(true);

        for (int i = 0; i < _cards.Count; i++)
        {
            AugmentData data = (cards != null && i < cards.Count) ? cards[i] : null;
            _cards[i].SetVisible(data != null);
            if (data == null)
            {
                continue;
            }

            _cards[i].SetData(data);
            _cards[i].SetRerollInteractable(rerollsLeft > 0);
        }

        UpdateRerollsLabel(rerollsLeft);
    }

    /// <summary>
    /// index의 카드를 data로 교체해 표시하고, 그 카드의 리롤 버튼을 비활성화한다.
    /// 나머지 카드의 리롤 버튼과 라벨은 남은 횟수 rerollsLeft에 맞춘다.
    /// </summary>
    public void ReplaceCard(int index, AugmentData data, int rerollsLeft)
    {
        if (index < 0 || index >= _cards.Count)
        {
            return;
        }

        _cards[index].SetVisible(true);
        _cards[index].SetData(data);
        _cards[index].SetRerollInteractable(false);

        for (int i = 0; i < _cards.Count; i++)
        {
            if (i != index)
            {
                _cards[i].SetRerollInteractable(rerollsLeft > 0);
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
    /// index번 카드의 선택 입력을 바인딩된 handler의 HandlePickClicked로 위임한다.
    /// </summary>
    public void HandleCardSelected(int index)
    {
        _handler?.HandlePickClicked(index);
    }

    /// <summary>
    /// index번 카드의 리롤 입력을 바인딩된 handler의 HandleRerollClicked로 위임한다.
    /// </summary>
    public void HandleCardRerolled(int index)
    {
        _handler?.HandleRerollClicked(index);
    }

    /// <summary>
    /// 남은 리롤 수 라벨 텍스트를 rerollsLeft 기준으로 갱신한다.
    /// </summary>
    private void UpdateRerollsLabel(int rerollsLeft)
    {
        _rerollsLabel.text = $"남은 리롤: {rerollsLeft}";
    }
}
