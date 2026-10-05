using UnityEngine;

[CreateAssetMenu(menuName = "Augment/Effects/AbsorbComboHealEffect")]
public class AbsorbComboHealEffect : AugmentEffect, IHealingSource
{
    [Header("Absorb Combo Heal")]
    [SerializeField, Min(1)] private int _requiredAbsorbs = 8;
    [SerializeField, Min(1)] private int _healAmount = 1;

    private AbsortionAbility_Sejin _absorptionAbility;
    private PlayerHp _playerHp;
    private int _absorbedCount;
    private bool _isCounting;
    private bool _healedThisCast;

    // 증강 적용 시 흡수 시작과 종료 이벤트를 구독한다.
    public override void OnApply()
    {
        OnRunReset();

        _absorptionAbility = FindFirstObjectByType<AbsortionAbility_Sejin>();
        _playerHp = FindFirstObjectByType<PlayerHp>();

        if (_absorptionAbility != null)
        {
            _absorptionAbility.OnAbsorbStarted += HandleAbsorbStarted;
            _absorptionAbility.OnAbsorbEnded += HandleAbsorbEnded;
        }
    }

    // 한 번의 흡수에서 성공한 처치를 세고 목표 수에 도달하면 한 번 회복한다.
    public override void OnEnemyAbsorbed(Enemy enemy)
    {
        if (!_isCounting || _healedThisCast)
        {
            return;
        }

        _absorbedCount++;
        if (_absorbedCount < _requiredAbsorbs)
        {
            return;
        }

        _healedThisCast = true;

        // 만체력으로 회복되지 않아도 이번 흡수의 발동 기회는 소모한다.
        if (_playerHp != null)
        {
            _playerHp.ReceiveHealing(
                new HealingInfo(_healAmount, HealingKind.AbsorbComboHeal, this));
        }
    }

    // 새 흡수가 시작되면 처치 수와 회복 여부를 초기화한다.
    private void HandleAbsorbStarted()
    {
        _absorbedCount = 0;
        _healedThisCast = false;
        _isCounting = true;
    }

    // 흡수가 끝나면 이번 시전의 집계를 종료한다.
    private void HandleAbsorbEnded()
    {
        _isCounting = false;
        _absorbedCount = 0;
        _healedThisCast = false;
    }

    // 새 판에서는 이전 흡수 이벤트 구독과 집계 상태를 정리한다.
    public override void OnRunReset()
    {
        if (_absorptionAbility != null)
        {
            _absorptionAbility.OnAbsorbStarted -= HandleAbsorbStarted;
            _absorptionAbility.OnAbsorbEnded -= HandleAbsorbEnded;
        }

        _absorptionAbility = null;
        _playerHp = null;
        _absorbedCount = 0;
        _isCounting = false;
        _healedThisCast = false;
    }
}