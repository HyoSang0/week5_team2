using System.Collections.Generic;

using UnityEngine;

/// <summary>
/// 지정한 시점에 증강 선택 UI를 열고 선택된 증강을 PlayerStats에 적용하는 런타임 관리자.
/// AugmentSystem 프리팹의 루트에 PlayerStats와 함께 배치한다.
/// </summary>
public class AugmentManager : MonoBehaviour, IAugmentSelectionHandler
{
    private const int CARD_COUNT = 3;

    // 회차별 3회 픽의 등급 조합과 선택 가중치다. Start에서 1개 조합을 뽑아 판 동안 고정한다.
    private static readonly AugmentTier[][] _patternTiers =
    {
        new AugmentTier[] { AugmentTier.Silver, AugmentTier.Gold, AugmentTier.Gold },
        new AugmentTier[] { AugmentTier.Silver, AugmentTier.Silver, AugmentTier.Gold },
        new AugmentTier[] { AugmentTier.Gold, AugmentTier.Gold, AugmentTier.Prismatic },
        new AugmentTier[] { AugmentTier.Prismatic, AugmentTier.Gold, AugmentTier.Prismatic },
    };

    private static readonly int[] _patternWeights = { 50, 25, 20, 5 };

    private static readonly AugmentTier[] _allTiers =
    {
        AugmentTier.Silver,
        AugmentTier.Gold,
        AugmentTier.Prismatic,
    };

    [Header("References")]
    [SerializeField] private AugmentDatabase _database;
    [SerializeField] private AugmentSelectionView _selectionView;
    [SerializeField] private AugmentHudView _hudView;
    [SerializeField] private PlayerHp _playerHp;

    [Header("Pick Schedule")]
    [SerializeField] private float[] _pickTimes = { 10f, 25f, 40f };
    [SerializeField] private int _totalRerolls = 3;

    [Header("Runtime State")]
    private readonly List<AugmentData> _owned = new List<AugmentData>();
    private readonly List<AugmentEffect> _subscribedEffects = new List<AugmentEffect>();
    private readonly AugmentData[] _currentCards = new AugmentData[CARD_COUNT];
    private readonly bool[] _cardRerolled = new bool[CARD_COUNT];
    private AugmentTier[] _runPattern;
    private AugmentTier _currentTier;
    private float _elapsed;
    private int _nextPickIndex;
    private int _rerollsLeft;
    private float _savedTimeScale = 1f;

    void Awake()
    {
        _selectionView.Bind(this);
    }

    void Start()
    {
        if (_database != null)
        {
            foreach (AugmentData data in _database.Augments)
            {
                if (data != null)
                {
                    data.Effect?.OnRunReset();
                }
            }
        }

        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.ResetAll();
        }

        _owned.Clear();
        _subscribedEffects.Clear();
        _elapsed = 0f;
        _nextPickIndex = 0;
        _rerollsLeft = _totalRerolls;
        _runPattern = RollTierPattern();
    }

    void Update()
    {
        _elapsed += Time.deltaTime;

        if (!AugmentSelection.IsOpen
            && _nextPickIndex < _pickTimes.Length
            && _elapsed >= _pickTimes[_nextPickIndex])
        {
            OpenSelection();
        }
    }

    /// <summary>
    /// OnDestroy에서 이벤트 구독을 해제하고 전역 선택 상태를 닫힌 상태로 되돌린다.
    /// </summary>
    private void OnDestroy()
    {
        foreach (AugmentEffect effect in _subscribedEffects)
        {
            if (effect != null)
            {
                AugmentEvents.OnEnemyKilled -= effect.OnEnemyKilled;
                AugmentEvents.OnEnemyAbsorbed -= effect.OnEnemyAbsorbed;
            }
        }

        _subscribedEffects.Clear();
        AugmentSelection.IsOpen = false;
    }

    /// <summary>
    /// 등급 조합 가중치(_patternWeights)에서 하나가 선택된 조합을 반환한다.
    /// 실패는 없으며 항상 _patternTiers의 한 조합을 반환한다.
    /// </summary>
    private static AugmentTier[] RollTierPattern()
    {
        int total = 0;
        foreach (int weight in _patternWeights)
        {
            total += weight;
        }

        int roll = UnityEngine.Random.Range(0, total);
        for (int i = 0; i < _patternWeights.Length; i++)
        {
            if (roll < _patternWeights[i])
            {
                return _patternTiers[i];
            }

            roll -= _patternWeights[i];
        }

        return _patternTiers[0];
    }

    /// <summary>
    /// 이번 회차 등급으로 카드를 추첨하고 UI를 연다.
    /// Time.timeScale을 저장한 뒤 0으로 바꾸고 AugmentSelection.IsOpen을 true로 설정한다.
    /// </summary>
    private void OpenSelection()
    {
        _currentTier = _runPattern[Mathf.Min(_nextPickIndex, _runPattern.Length - 1)];
        FillCards(_currentTier);

        if (CountCards() < CARD_COUNT)
        {
            foreach (AugmentTier otherTier in _allTiers)
            {
                if (otherTier != _currentTier)
                {
                    FillCards(otherTier);
                }
            }
        }

        if (CountCards() == 0)
        {
            _nextPickIndex++;
            return;
        }

        AugmentSelection.IsOpen = true;
        _savedTimeScale = Time.timeScale;
        Time.timeScale = 0f;

        _selectionView.ShowCards(_currentCards, _rerollsLeft);
    }

    /// <summary>
    /// 빈 슬롯에 tier의 후보(보유/충돌 제외, 현재 표시 카드 제외)를 무작위로 채운다.
    /// 변경된 카드는 _currentCards에 저장한다.
    /// </summary>
    private void FillCards(AugmentTier tier)
    {
        List<AugmentData> candidates = _database.GetCandidates(tier, _owned);
        candidates.RemoveAll(card => card == null || IsDisplayed(card));

        for (int i = 0; i < CARD_COUNT && candidates.Count > 0; i++)
        {
            if (_currentCards[i] != null)
            {
                continue;
            }

            int pick = UnityEngine.Random.Range(0, candidates.Count);
            _currentCards[i] = candidates[pick];
            candidates.RemoveAt(pick);
        }
    }

    /// <summary>
    /// index의 카드가 선택됐다는 UI 입력을 처리한다.
    /// 증강을 owned에 추가하고 PlayerStats에 수정자를 적용한 뒤 UI를 닫고 시간을 복원한다.
    /// </summary>
    public void HandlePickClicked(int index)
    {
        if (index < 0 || index >= CARD_COUNT || _currentCards[index] == null)
        {
            return;
        }

        AugmentData data = _currentCards[index];
        _owned.Add(data);

        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.AddModifiers(data.Modifiers);
        }

        AugmentEffect effect = data.Effect;
        if (effect != null)
        {
            effect.OnApply();
            AugmentEvents.OnEnemyKilled += effect.OnEnemyKilled;
            AugmentEvents.OnEnemyAbsorbed += effect.OnEnemyAbsorbed;
            _subscribedEffects.Add(effect);
        }

        _selectionView.Hide();
        _hudView.AddOwned(data);
        CloseSelection();
        _playerHp.ApplyHitInvincibility(1f);


        for (int i = 0; i < CARD_COUNT; i++)
        {
            _currentCards[i] = null;
            _cardRerolled[i] = false;
        }

        _nextPickIndex++;
    }

    /// <summary>
    /// index의 카드를 리롤한다는 UI 입력을 처리한다.
    /// 남은 리롤이 있고 해당 카드가 아직 리롤되지 않았으며 대체 후보가 있을 때만 같은 등급에서 1장을 교체하고 횟수를 차감한다.
    /// </summary>
    public void HandleRerollClicked(int index)
    {
        if (index < 0 || index >= CARD_COUNT || _rerollsLeft <= 0 || _cardRerolled[index])
        {
            return;
        }

        List<AugmentData> candidates = _database.GetCandidates(_currentTier, _owned);
        candidates.RemoveAll(card => card == null || IsDisplayed(card));

        if (candidates.Count == 0)
        {
            return;
        }

        AugmentData replacement = candidates[UnityEngine.Random.Range(0, candidates.Count)];
        _currentCards[index] = replacement;
        _cardRerolled[index] = true;
        _rerollsLeft--;

        _selectionView.ReplaceCard(index, replacement, _rerollsLeft);
    }

    /// <summary>
    /// 선택 UI를 숨기고 Time.timeScale을 저장값으로 복원한 뒤 AugmentSelection.IsOpen을 false로 되돌린다.
    /// </summary>
    private void CloseSelection()
    {
        Time.timeScale = _savedTimeScale;
        AugmentSelection.IsOpen = false;
    }

    /// <summary>
    /// data가 현재 표시 중인 3장의 카드 중 하나인지 나타낸다.
    /// </summary>
    private bool IsDisplayed(AugmentData data)
    {
        for (int i = 0; i < CARD_COUNT; i++)
        {
            if (_currentCards[i] == data)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 현재 표시 중인 비어 있지 않은 카드의 장수를 반환한다.
    /// </summary>
    private int CountCards()
    {
        int count = 0;
        for (int i = 0; i < CARD_COUNT; i++)
        {
            if (_currentCards[i] != null)
            {
                count++;
            }
        }

        return count;
    }
}
