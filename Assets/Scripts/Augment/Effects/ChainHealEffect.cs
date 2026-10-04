using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 연쇄 회복 증강 효과. 한 돌진에서 5번째 처치를 달성하면 HP를 1 회복한다.
/// </summary>
[CreateAssetMenu(menuName = "Augment/Effects/ChainHealEffect")]
public class ChainHealEffect : AugmentEffect, IHealingSource
{
    private const int HEAL_TRIGGER_COUNT = 5;

    [SerializeField] private int _healAmount = 1;

    private RushAbility_Sejin _rush;
    private PlayerHp _playerHp;
    private UnityAction _onRushStarted;
    private int _killCount;

    /// <summary>
    /// 증강 적용 시 플레이어 컴포넌트를 찾고 돌진 시작 리스너를 등록해 킬 카운트를 0으로 되돌린다.
    /// </summary>
    public override void OnApply()
    {
        _rush = FindFirstObjectByType<RushAbility_Sejin>();
        _playerHp = FindFirstObjectByType<PlayerHp>();

        if (_rush != null)
        {
            if (_onRushStarted == null)
            {
                _onRushStarted = new UnityAction(ResetKillCount);
            }

            _rush.onStartRush.AddListener(_onRushStarted);
        }

        _killCount = 0;
    }

    /// <summary>
    /// 적이 처치되면 실제 돌진 중 발생한 처치만 카운트해 한 돌진에서 처음 5번째가 되는 순간만 HP를 healAmount 회복한다.
    /// </summary>
    public override void OnEnemyKilled(Enemy enemy)
    {
        // 돌진 이동 중이 아닌 처치(연쇄/충돌 등)는 연쇄 카운트에 포함하지 않는다.
        if (_rush == null || !_rush.isRushing)
        {
            return;
        }

        _killCount++;

        if (_killCount == HEAL_TRIGGER_COUNT && _playerHp != null)
        {
            // 만체력이라 실제 회복이 일어나지 않았더라도 5번째 처치 트리거는 그대로 소비한다.
            IHealable healTarget = _playerHp;
            healTarget.ReceiveHealing(new HealingInfo(_healAmount, HealingKind.ChainHeal, this));
        }
    }

    /// <summary>
    /// 새 판 시작 시 돌진 시작 리스너를 해제하고 캐시한 참조와 카운트를 초기화한다.
    /// </summary>
    public override void OnRunReset()
    {
        if (_rush != null && _onRushStarted != null)
        {
            _rush.onStartRush.RemoveListener(_onRushStarted);
        }

        _rush = null;
        _playerHp = null;
        _killCount = 0;
    }

    /// <summary>
    /// 돌진 시작 시 호출되어 연쇄 처치 카운트를 0으로 되돌린다.
    /// </summary>
    private void ResetKillCount()
    {
        _killCount = 0;
    }
}
