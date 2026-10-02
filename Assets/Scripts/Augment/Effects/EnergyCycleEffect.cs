using UnityEngine;

/// <summary>
/// 에너지 순환 증강 효과. 적을 처치할 때마다 흡수 스태미나를 충전한다.
/// </summary>
[CreateAssetMenu(menuName = "Augment/Effects/EnergyCycleEffect")]
public class EnergyCycleEffect : AugmentEffect
{
    [SerializeField] private float _staminaPerKill = 5f;

    private AbsortionAbility_Sejin _absortion;

    /// <summary>
    /// 증강 적용 시신의 플레이어 흡수 컴포넌트를 찾아 보관한다.
    /// </summary>
    public override void OnApply()
    {
        _absortion = FindFirstObjectByType<AbsortionAbility_Sejin>();
    }

    /// <summary>
    /// 적이 처치되면 enemy와 무관하게 흡수 스태미나를 staminaPerKill만큼 충전한다.
    /// </summary>
    public override void OnEnemyKilled(Enemy enemy)
    {
        if (_absortion != null)
        {
            _absortion.AddStamina(_staminaPerKill);
        }
    }

    /// <summary>
    /// 새 판 시작 시 캐시한 흡수 컴포넌트 참조를 비운다.
    /// </summary>
    public override void OnRunReset()
    {
        _absortion = null;
    }
}
