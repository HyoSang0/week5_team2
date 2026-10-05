using UnityEngine;

[CreateAssetMenu(menuName = "Augment/Effects/AbsorbHitIgnoreEffect")]
public class AbsorbHitIgnoreEffect : AugmentEffect
{
    [Header("Absorb Hit Ignore")]
    [SerializeField, Min(0f)] private float _cooldownSeconds = 15f;

    private AbsortionAbility_Sejin _absorbAbility;
    private PlayerHp _playerHp;
    private float _nextReadyTime;

    // 증강 선택 시 흡수 시작과 종료 이벤트를 구독한다.
    public override void OnApply()
    {
        OnRunReset();

        _absorbAbility = FindFirstObjectByType<AbsortionAbility_Sejin>();
        _playerHp = FindFirstObjectByType<PlayerHp>();

        if (_absorbAbility != null)
        {
            _absorbAbility.OnAbsorbStarted += HandleAbsorbStarted;
            _absorbAbility.OnAbsorbEnded += HandleAbsorbEnded;
        }
    }

    // 쿨타임이 끝났다면 이번 흡수 시전에 피격 무시 1회를 부여한다.
    private void HandleAbsorbStarted()
    {
        if (_playerHp == null || Time.time < _nextReadyTime)
        {
            return;
        }

        if (_playerHp.TryGrantAbsorbHitIgnore())
        {
            _nextReadyTime = Time.time + _cooldownSeconds;
        }
    }

    // 흡수가 끝나면 사용하지 않은 피격 무시를 제거한다.
    private void HandleAbsorbEnded()
    {
        if (_playerHp != null)
        {
            _playerHp.ClearAbsorbHitIgnore();
        }
    }

    // 새 판이 시작되면 이벤트 구독과 이전 판의 상태를 정리한다.
    public override void OnRunReset()
    {
        if (_absorbAbility != null)
        {
            _absorbAbility.OnAbsorbStarted -= HandleAbsorbStarted;
            _absorbAbility.OnAbsorbEnded -= HandleAbsorbEnded;
        }

        if (_playerHp != null)
        {
            _playerHp.ClearAbsorbHitIgnore();
        }

        _absorbAbility = null;
        _playerHp = null;
        _nextReadyTime = 0f;
    }
}