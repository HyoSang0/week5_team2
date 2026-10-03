using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

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

    [Header("Gamepad Navigation")]
    [SerializeField] private UIDefaultSelection _defaultSelection;

    private readonly List<AugmentCardView> _cards = new List<AugmentCardView>();
    private readonly bool[] _rerolled = new bool[CARD_COUNT];
    private IAugmentSelectionHandler _handler;

    void Awake()
    {
        for (int i = 0; i < CARD_COUNT; i++)
        {
            AugmentCardView card = Instantiate(_cardPrefab, _cardContainer);
            card.Bind(i, this);
            _cards.Add(card);
        }

        SetupNavigation();
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
    /// 리롤 여부를 모두 초기화하고 첫 보이는 카드를 기본 선택 대상으로만 지정한다(즉시 선택하지 않는다).
    /// </summary>
    public void ShowCards(IReadOnlyList<AugmentData> cards, int rerollsLeft)
    {
        _selectPanel.SetActive(true);

        for (int i = 0; i < _rerolled.Length; i++)
        {
            _rerolled[i] = false;
        }

        for (int i = 0; i < _cards.Count; i++)
        {
            AugmentData data = (cards != null && i < cards.Count) ? cards[i] : null;
            _cards[i].SetVisible(data != null);
            if (data == null)
            {
                continue;
            }

            _cards[i].SetData(data);
            _cards[i].SetRerollInteractable(rerollsLeft > 0 && !_rerolled[i]);
        }

        SetupNavigation();
        UpdateRerollsLabel(rerollsLeft);
        SelectFirstVisibleCard();
    }

    /// <summary>
    /// index의 카드를 data로 교체해 표시하고 _rerolled[index]를 true로 기록해 그 카드의 리롤 버튼을 비활성화한다.
    /// 나머지 카드의 리롤 버튼은 남은 횟수 rerollsLeft와 각 카드의 리롤 여부로 갱신하고,
    /// 패드로 조작 중일 때만 교체된 카드를 기본 선택으로 되돌린다.
    /// </summary>
    public void ReplaceCard(int index, AugmentData data, int rerollsLeft)
    {
        if (index < 0 || index >= _cards.Count)
        {
            return;
        }

        _cards[index].SetVisible(true);
        _cards[index].SetData(data);
        _rerolled[index] = true;

        for (int i = 0; i < _cards.Count; i++)
        {
            _cards[i].SetRerollInteractable(rerollsLeft > 0 && !_rerolled[i]);
        }

        UpdateRerollsLabel(rerollsLeft);

        if (_defaultSelection != null)
        {
            _defaultSelection.SetDefault(_cards[index].SelectButton);
            _defaultSelection.SelectDefaultIfNavigating();
        }
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
    /// _defaultSelection이 있고 입력 잠금 중이면 무시한다.
    /// </summary>
    public void HandleCardSelected(int index)
    {
        if (_defaultSelection != null && _defaultSelection.IsInputLocked)
        {
            return;
        }

        _handler?.HandlePickClicked(index);
    }

    /// <summary>
    /// index번 카드의 리롤 입력을 바인딩된 handler의 HandleRerollClicked로 위임한다.
    /// _defaultSelection이 있고 입력 잠금 중이면 무시한다.
    /// </summary>
    public void HandleCardRerolled(int index)
    {
        if (_defaultSelection != null && _defaultSelection.IsInputLocked)
        {
            return;
        }

        _handler?.HandleRerollClicked(index);
    }

    /// <summary>
    /// 보이는 카드만 대상으로 선택/리롤 버튼의 Navigation을 Explicit으로 연결한다.
    /// 선택 버튼은 좌/우가 이웃 카드의 선택 버튼(양 끝은 없음), 아래가 같은 카드의 리롤 버튼,
    /// 리롤 버튼은 위가 같은 카드의 선택 버튼, 좌/우가 이웃 카드의 리롤 버튼이다.
    /// </summary>
    private void SetupNavigation()
    {
        List<AugmentCardView> visible = new List<AugmentCardView>();
        for (int i = 0; i < _cards.Count; i++)
        {
            if (_cards[i].gameObject.activeSelf)
            {
                visible.Add(_cards[i]);
            }
        }

        for (int k = 0; k < visible.Count; k++)
        {
            Button neighborSelectLeft = k > 0 ? visible[k - 1].SelectButton : null;
            Button neighborSelectRight = k < visible.Count - 1 ? visible[k + 1].SelectButton : null;

            Navigation selectNav = visible[k].SelectButton.navigation;
            selectNav.mode = Navigation.Mode.Explicit;
            selectNav.selectOnLeft = neighborSelectLeft;
            selectNav.selectOnRight = neighborSelectRight;
            selectNav.selectOnUp = null;
            selectNav.selectOnDown = visible[k].RerollButton;
            visible[k].SelectButton.navigation = selectNav;

            Navigation rerollNav = visible[k].RerollButton.navigation;
            rerollNav.mode = Navigation.Mode.Explicit;
            rerollNav.selectOnUp = visible[k].SelectButton;
            rerollNav.selectOnLeft = k > 0 ? visible[k - 1].RerollButton : null;
            rerollNav.selectOnRight = k < visible.Count - 1 ? visible[k + 1].RerollButton : null;
            rerollNav.selectOnDown = null;
            visible[k].RerollButton.navigation = rerollNav;
        }
    }

    /// <summary>
    /// 첫 번째로 보이는 카드의 선택 버튼을 기본 선택 대상으로만 지정하고 실제 선택은 하지 않는다.
    /// 실제 선택은 패드 입력이 감지됐을 때 UIDefaultSelection이 수행한다.
    /// _defaultSelection이 없거나 보이는 카드가 없으면 아무것도 하지 않는다.
    /// </summary>
    private void SelectFirstVisibleCard()
    {
        if (_defaultSelection == null)
        {
            return;
        }

        for (int i = 0; i < _cards.Count; i++)
        {
            if (_cards[i].gameObject.activeSelf)
            {
                _defaultSelection.SetDefault(_cards[i].SelectButton);
                return;
            }
        }
    }

    /// <summary>
    /// 남은 리롤 수 라벨 텍스트를 rerollsLeft 기준으로 갱신한다.
    /// </summary>
    private void UpdateRerollsLabel(int rerollsLeft)
    {
        _rerollsLabel.text = $"남은 리롤: {rerollsLeft}";
    }
}
