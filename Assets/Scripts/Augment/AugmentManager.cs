using System.Collections.Generic;

using UnityEngine;

/// <summary>
/// 지정한 시점에 증강 선택 UI를 열고 선택된 증강을 PlayerStats에 적용하는 런타임 관리자.
/// MainScene의 UiManager 오브젝트에 PlayerStats와 함께 배치한다.
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

    [Header("Pick Schedule")]
    [SerializeField] private float[] _pickTimes = { 10f, 25f, 40f };
    [SerializeField] private int _totalRerolls = 3;

    [Header("Runtime State")]
    private readonly List<AugmentData> _owned = new List<AugmentData>();
    private readonly List<AugmentEffect> _subscribedEffects = new List<AugmentEffect>();
    private readonly AugmentData[] _currentCards = new AugmentData[CARD_COUNT];
    private readonly bool[] _cardRerolled = new bool[CARD_COUNT];
    private readonly HashSet<AugmentData> _seenThisSelection = new HashSet<AugmentData>();
    private AugmentTier[] _runPattern;
    private AugmentTier _currentTier;
    private float _elapsed;
    private int _nextPickIndex;
    private int _rerollsLeft;

    void Awake()
    {
        _selectionView.Bind(this);
    }

    void Start()
    {
        // 재시작(씬 재로드) 전에 이전 런에서 남은 이벤트 구독을 먼저 해제한다.
        // 효과는 ScriptableObject 에셋이라 씬 종료 후에도 정적 이벤트에 구독이 남을 수 있다.
        if (_database != null)
        {
            foreach (AugmentData data in _database.Augments)
            {
                if (data != null && data.Effect != null)
                {
                    UnsubscribeEffect(data.Effect);
                    data.Effect.OnRunReset();
                }
            }
        }

        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.ResetAll();
        }

        _owned.Clear();
        _subscribedEffects.Clear();
        _hudView.ClearOwned();
        _elapsed = 0f;
        _nextPickIndex = 0;
        _rerollsLeft = _totalRerolls;
        _runPattern = RollTierPattern();
    }

    void Update()
    {
        _elapsed += Time.deltaTime;

        if (!AugmentSelection.IsOpen && _nextPickIndex < _pickTimes.Length && _elapsed >= _pickTimes[_nextPickIndex])
        {
            OpenSelection();
        }
    }

    /// <summary>
    /// OnDestroy에서 이벤트 구독을 해제하고 선택 UI가 열린 상태에서 파괴됐으면 AugmentSelection 원인의 일시정지를 해제한다.
    /// </summary>
    private void OnDestroy()
    {
        UnsubscribeSubscribedEffects();

        if (AugmentSelection.IsOpen)
        {
            GamePause.Resume(PauseReason.AugmentSelection);
            AugmentSelection.IsOpen = false;
        }
    }

    /// <summary>
    /// _subscribedEffects에 기록된 효과들의 OnEnemyKilled/OnEnemyAbsorbed 구독을 해제하고 목록을 비운다.
    /// </summary>
    private void UnsubscribeSubscribedEffects()
    {
        foreach (AugmentEffect effect in _subscribedEffects)
        {
            if (effect != null)
            {
                UnsubscribeEffect(effect);
            }
        }

        _subscribedEffects.Clear();
    }

    /// <summary>
    /// effect의 OnEnemyKilled/OnEnemyAbsorbed 구독을 해제한다. 구독되어 있지 않으면 -= 연산자는 아무 동작을 하지 않는다.
    /// </summary>
    private static void UnsubscribeEffect(AugmentEffect effect)
    {
        AugmentEvents.OnEnemyKilled -= effect.OnEnemyKilled;
        AugmentEvents.OnEnemyAbsorbed -= effect.OnEnemyAbsorbed;
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
    /// GamePause로 AugmentSelection 원인의 일시정지를 걸고 AugmentSelection.IsOpen을 true로 설정한다.
    /// </summary>
    private void OpenSelection()
    {
        _currentTier = _runPattern[Mathf.Min(_nextPickIndex, _runPattern.Length - 1)];
        _seenThisSelection.Clear();
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
        GamePause.Pause(PauseReason.AugmentSelection);

        _selectionView.ShowCards(_currentCards, _rerollsLeft);
    }

    /// <summary>
    /// 빈 슬롯에 tier의 후보(보유/충돌 제외, 현재 표시 카드와 충돌하거나 이번 선택 창에서 이미 보여준 카드 제외)를 무작위로 채운다.
    /// 카드를 하나 채울 때마다 남은 후보에서 그 카드와 ConflictsWith인 항목을 제거한다.
    /// 채운 카드는 _currentCards에 저장하고 _seenThisSelection에 추가한다.
    /// </summary>
    private void FillCards(AugmentTier tier)
    {
        List<AugmentData> candidates = _database.GetCandidates(tier, _owned);
        candidates.RemoveAll(card => card == null || IsDisplayed(card) || ConflictsWithDisplayed(card) || _seenThisSelection.Contains(card));

        for (int i = 0; i < CARD_COUNT && candidates.Count > 0; i++)
        {
            if (_currentCards[i] != null)
            {
                continue;
            }

            int pick = UnityEngine.Random.Range(0, candidates.Count);
            AugmentData selected = candidates[pick];
            _currentCards[i] = selected;
            _seenThisSelection.Add(selected);

            // 새로 채운 카드와 참조가 같거나 ConflictsWith인 후보는 같은 창에 함께 채우지 않는다.
            candidates.RemoveAll(card => card.ConflictsWith(selected));
        }
    }

    /// <summary>
    /// index의 카드가 선택됐다는 UI 입력을 처리한다.
    /// 증강을 owned에 추가하고 PlayerStats에 수정자를 적용한 뒤 UI를 닫고 일시정지를 해제한다.
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

            // 같은 효과를 owning한 증강을 다시 선택해도 이벤트 구독은 한 번만 등록한다.
            if (!_subscribedEffects.Contains(effect))
            {
                AugmentEvents.OnEnemyKilled += effect.OnEnemyKilled;
                AugmentEvents.OnEnemyAbsorbed += effect.OnEnemyAbsorbed;
                _subscribedEffects.Add(effect);
            }
        }

        _selectionView.Hide();
        _hudView.AddOwned(data);
        CloseSelection();

        for (int i = 0; i < CARD_COUNT; i++)
        {
            _currentCards[i] = null;
            _cardRerolled[i] = false;
        }

        _nextPickIndex++;
    }

    /// <summary>
    /// index의 카드를 리롤한다는 UI 입력을 처리한다.
    /// 남은 리롤이 있고 해당 카드가 아직 리롤되지 않았으며 이번 선택 창에서 아직 보여주지 않은 대체 후보가 있을 때만 1장을 교체하고 횟수를 차감한다.
    /// 대체 후보가 없으면 아무것도 하지 않는다.
    /// </summary>
    public void HandleRerollClicked(int index)
    {
        if (index < 0 || index >= CARD_COUNT || _rerollsLeft <= 0 || _cardRerolled[index])
        {
            return;
        }

        AugmentData replacement = FindRerollReplacement();
        if (replacement == null)
        {
            return;
        }

        _currentCards[index] = replacement;
        _seenThisSelection.Add(replacement);
        _cardRerolled[index] = true;
        _rerollsLeft--;

        _selectionView.ReplaceCard(index, replacement, _rerollsLeft);
    }

    /// <summary>
    /// 리롤 대체 카드를 현재 등급에서 찾고, 후보가 없으면 _allTiers 순서로 다른 등급에서 찾는다.
    /// 보유 중이거나 이번 선택 창에서 이미 보여준 증강은 제외한다.
    /// 후보가 없으면 null을 반환한다.
    /// </summary>
    private AugmentData FindRerollReplacement()
    {
        AugmentData replacement = PickRerollCandidate(_currentTier);
        if (replacement != null)
        {
            return replacement;
        }

        foreach (AugmentTier tier in _allTiers)
        {
            if (tier == _currentTier)
            {
                continue;
            }

            replacement = PickRerollCandidate(tier);
            if (replacement != null)
            {
                return replacement;
            }
        }

        return null;
    }

    /// <summary>
    /// tier의 후보 중 보유·현재 표시·현재 표시 카드와 충돌·이미 보여준 증강을 제외한 1장을 무작위로 반환한다.
    /// 후보가 없으면 null을 반환한다.
    /// </summary>
    private AugmentData PickRerollCandidate(AugmentTier tier)
    {
        List<AugmentData> candidates = _database.GetCandidates(tier, _owned);
        candidates.RemoveAll(card => card == null || IsDisplayed(card) || ConflictsWithDisplayed(card) || _seenThisSelection.Contains(card));

        if (candidates.Count == 0)
        {
            return null;
        }

        return candidates[UnityEngine.Random.Range(0, candidates.Count)];
    }

    /// <summary>
    /// 선택 UI를 숨기고 GamePause로 AugmentSelection 원인의 일시정지를 해제한 뒤 AugmentSelection.IsOpen을 false로 되돌린다.
    /// </summary>
    private void CloseSelection()
    {
        GamePause.Resume(PauseReason.AugmentSelection);
        AugmentSelection.IsOpen = false;
    }

    /// <summary>
    /// data가 현재 표시 중인 3장의 카드 중 하나와 ConflictsWith인지 나타낸다.
    /// </summary>
    private bool ConflictsWithDisplayed(AugmentData data)
    {
        for (int i = 0; i < CARD_COUNT; i++)
        {
            if (_currentCards[i] != null && data.ConflictsWith(_currentCards[i]))
            {
                return true;
            }
        }

        return false;
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
