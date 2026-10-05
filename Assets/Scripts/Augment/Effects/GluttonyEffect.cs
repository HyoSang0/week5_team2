using UnityEngine;

/// <summary>
/// 과식 증강 효과. 한 번의 흡수 활성화에서 성공적으로 흡수한 적 수가 임계값 이상이면 활성 종료 시 HP를 1 회복한다.
/// </summary>
[CreateAssetMenu(menuName = "Augment/Effects/GluttonyEffect")]
public class GluttonyEffect : AugmentEffect, IHealingSource
{
    private const int HEAL_AMOUNT = 1;

    [Header("과식 설정")]
    [SerializeField] private int _requiredAbsorbCount = 10;

    [Header("Runtime State")]
    private PlayerHp _playerHp;
    private int _absorbCount;
    private bool _isActivationActive;

    /// <summary>
    /// 증강 적용 시 플레이어의 체력 컴포넌트를 찾아 보관하고 흡수 활성화 시작·종료 이벤트를 구독하며 집계 상태를 초기화한다.
    /// PlayerHp를 _playerHp에 저장하고, _absorbCount와 _isActivationActive를 0·false로 되돌린다.
    /// </summary>
    public override void OnApply()
    {
        _playerHp = FindFirstObjectByType<PlayerHp>();

        AugmentEvents.OnAbsorptionStarted += HandleAbsorptionStarted;
        AugmentEvents.OnAbsorptionEnded += HandleAbsorptionEnded;

        _absorbCount = 0;
        _isActivationActive = false;
    }

    /// <summary>
    /// 적이 흡수될 때 현재 흡수 활성화가 진행 중이면 성공 흡수 수를 센다.
    /// enemy는 흡수된 적이며, 집계된 수는 _absorbCount에 누적된다.
    /// </summary>
    public override void OnEnemyAbsorbed(Enemy enemy)
    {
        // 활성화 밖에서 발화한 흡수는 이번 활성화 집계에 포함하지 않는다.
        if (!_isActivationActive)
        {
            return;
        }

        _absorbCount++;
    }

    /// <summary>
    /// 새 판 시작 시 흡수 활성화 이벤트 구독을 해제하고 캐시한 참조와 집계 상태를 초기화한다.
    /// </summary>
    public override void OnRunReset()
    {
        AugmentEvents.OnAbsorptionStarted -= HandleAbsorptionStarted;
        AugmentEvents.OnAbsorptionEnded -= HandleAbsorptionEnded;

        _playerHp = null;
        _absorbCount = 0;
        _isActivationActive = false;
    }

    /// <summary>
    /// 흡수 활성화 시작 통지를 받아 성공 흡수 수를 0으로 되돌리고 집계를 시작한다.
    /// _absorbCount를 0으로, _isActivationActive를 true로 변경한다.
    /// </summary>
    private void HandleAbsorptionStarted()
    {
        _absorbCount = 0;
        _isActivationActive = true;
    }

    /// <summary>
    /// 흡수 활성화 종료 통지를 받아 집계된 성공 흡수 수가 임계값 이상이면 HEAL_AMOUNT만큼 HP 회복을 1회 요청한다.
    /// 만체력 등으로 실제 회복이 일어나지 않았어도 트리거는 이번 활성화에서 1회만 요청하며, 집계 상태를 초기화한다.
    /// </summary>
    private void HandleAbsorptionEnded()
    {
        if (_isActivationActive && _absorbCount >= _requiredAbsorbCount && _playerHp != null)
        {
            IHealable healTarget = _playerHp;
            healTarget.ReceiveHealing(new HealingInfo(HEAL_AMOUNT, HealingKind.Gluttony, this));
        }

        _isActivationActive = false;
        _absorbCount = 0;
    }
}
