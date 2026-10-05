using UnityEngine;

[CreateAssetMenu(menuName = "Augment/Effects/StockpileShieldEffect")]
public class StockpileShieldEffect : AugmentEffect
{
    [Header("Stockpile Shield")]
    [SerializeField, Min(1)] private int _requiredAbsorbs = 20;
    [SerializeField, Min(0f)] private float _cooldownSeconds = 10f;

    private PlayerHp _playerHp;
    private int _absorbCount;
    private float _cooldownReadyTime;

    // 증강 선택 시 체력 컴포넌트를 찾고 흡수 횟수와 쿨다운을 초기화한다.
    public override void OnApply()
    {
        _playerHp = FindFirstObjectByType<PlayerHp>();
        _absorbCount = 0;
        _cooldownReadyTime = 0f;
    }

    // 보호막이 없고 쿨다운이 끝난 동안 흡수를 세어 한 번의 피해를 막는 보호막을 부여한다.
    public override void OnEnemyAbsorbed(Enemy enemy)
    {
        if (_playerHp == null || _playerHp.HasOneHitShield || Time.time < _cooldownReadyTime)
        {
            return;
        }

        _absorbCount++;
        if (_absorbCount < _requiredAbsorbs || !_playerHp.TryGrantOneHitShield())
        {
            return;
        }

        _absorbCount = 0;
        _cooldownReadyTime = Time.time + _cooldownSeconds;
    }

    // 새 판 시작 시 이전 플레이어 참조와 진행 중인 흡수 횟수를 비운다.
    public override void OnRunReset()
    {
        _playerHp = null;
        _absorbCount = 0;
        _cooldownReadyTime = 0f;
    }
}
