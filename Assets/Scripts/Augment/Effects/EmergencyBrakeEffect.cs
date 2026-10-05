using UnityEngine;

/// <summary>
/// 긴급 제동 증강 효과. 돌진 중 다시 돌진 입력을 하면 이동을 즉시 종료하고
/// 남은 거리 비율에 비례해 돌진 쿨타임의 최대 maxRefundPercent까지 환불한다.
/// </summary>
[CreateAssetMenu(menuName = "Augment/Effects/EmergencyBrakeEffect")]
public class EmergencyBrakeEffect : AugmentEffect
{
    [Header("Emergency Brake")]
    [SerializeField, Range(0f, 100f)] private float _maxRefundPercent = 50f;

    private RushAbility_Sejin _rush;

    /// <summary>
    /// 증강 적용 시 플레이어의 돌진 컴포넌트를 찾아 긴급 제동을 활성화한다.
    /// _maxRefundPercent를 환불 상한으로 전달한다.
    /// </summary>
    public override void OnApply()
    {
        _rush = FindFirstObjectByType<RushAbility_Sejin>();
        if (_rush != null)
        {
            _rush.EnableEmergencyBrake(_maxRefundPercent);
        }
    }

    /// <summary>
    /// 새 판 시작 시 캐시한 돌진 컴포넌트 참조를 비운다.
    /// 새 판의 RushAbility 컴포넌트는 긴급 제동 비활성 상태로 새로 생성된다.
    /// </summary>
    public override void OnRunReset()
    {
        _rush = null;
    }
}
