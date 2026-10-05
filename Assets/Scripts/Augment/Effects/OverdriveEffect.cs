using UnityEngine;

/// <summary>
/// 오버드라이브 증강 효과. 플레이어의 현재 체력 비율(currentHP/maxHP)에 비례해
/// 유효 돌진 거리를 최대 maxBonusPercent까지 증가시킨다. 체력 0이면 보너스 0,전부 체력이면 최대 보너스다.
/// </summary>
[CreateAssetMenu(menuName = "Augment/Effects/OverdriveEffect")]
public class OverdriveEffect : AugmentEffect
{
    [Header("Overdrive")]
    [SerializeField, Min(0f)] private float _maxBonusPercent = 50f;

    private RushAbility_Sejin _rush;
    private PlayerHp _playerHp;

    /// <summary>
    /// 증강 적용 시 돌진/체력 컴포넌트를 찾고 체력 변경 이벤트를 구독한 뒤 현재 보너스를 적용한다.
    /// </summary>
    public override void OnApply()
    {
        _rush = FindFirstObjectByType<RushAbility_Sejin>();
        _playerHp = FindFirstObjectByType<PlayerHp>();
        if (_playerHp != null)
        {
            _playerHp.OnHpChanged -= UpdateBonus;
            _playerHp.OnHpChanged += UpdateBonus;
        }
        UpdateBonus();
    }

    /// <summary>
    /// 새 판 시작 시 체력 변경 구독을 해제하고 캐시한 참조를 비운다.
    /// </summary>
    public override void OnRunReset()
    {
        if (_playerHp != null)
        {
            _playerHp.OnHpChanged -= UpdateBonus;
        }

        _rush = null;
        _playerHp = null;
    }

    /// <summary>
    /// 현재 playerHP/maxPlayerHP 비율에 _maxBonusPercent를 곱한 돌진 거리 보너스를 돌진 컴포넌트에 적용한다.
    /// maxPlayerHP가 0 이하이거나 참조가 없으면 아무 동작도 하지 않는다.
    /// </summary>
    private void UpdateBonus()
    {
        if (_rush == null || _playerHp == null || _playerHp.maxPlayerHP <= 0)
        {
            return;
        }

        float healthRatio = Mathf.Clamp01((float)_playerHp.playerHP / _playerHp.maxPlayerHP);
        _rush.SetOverdriveDistanceBonus(healthRatio * _maxBonusPercent);
    }
}
