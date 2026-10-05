using UnityEngine;

/// <summary>
/// 에너지 전환 증강 효과. 증강 선택 이후 흡수한 적 N기마다 유효 돌진 거리를 X% 증가시키며,
/// 선택 시점 이전의 흡수 실적은 기준값으로 제외하고 증가에 반영하지 않는다. 보너스는 +10%에서 제한된다.
/// </summary>
[CreateAssetMenu(menuName = "Augment/Effects/EnergyConversionEffect")]
public class EnergyConversionEffect : AugmentEffect
{
    private const float MAX_BONUS_PERCENT = 10f;

    [Header("Energy Conversion")]
    [SerializeField, Min(1)] private int _absorptionsPerStep = 10;
    [SerializeField, Min(0f)] private float _distancePercentPerStep = 2f;

    private RushAbility_Sejin _rush;
    private int _baselineAbsorbCount;

    /// <summary>
    /// 증강 적용 시 돌진 컴포넌트를 찾고 현재까지의 흡수 통계를 기준값으로 캡처한 뒤 보너스를 적용한다.
    /// </summary>
    public override void OnApply()
    {
        _rush = FindFirstObjectByType<RushAbility_Sejin>();
        _baselineAbsorbCount = StatisticsManager.Instance.GetCount(StatisticsManager.GameStatisticType.EnemyAbsorb);
        ApplyBonus();
    }

    /// <summary>
    /// 적이 흡수될 때마다 기준값 이후의 흡수 횟수로 보너스 단위를 계산해 돌진 거리 보너스를 갱신한다.
    /// </summary>
    public override void OnEnemyAbsorbed(Enemy enemy)
    {
        ApplyBonus();
    }

    /// <summary>
    /// 새 판 시작 시 캐시한 돌진 컴포넌트 참조와 흡수 기준값을 초기화한다.
    /// </summary>
    public override void OnRunReset()
    {
        _rush = null;
        _baselineAbsorbCount = 0;
    }

    /// <summary>
    /// 기준값 이후의 EnemyAbsorb 통계에서 _absorptionsPerStep 단위로 보너스 단위를 계산하고,
    /// MAX_BONUS_PERCENT로 제한한 돌진 거리 보너스를 돌진 컴포넌트에 적용한다.
    /// </summary>
    private void ApplyBonus()
    {
        if (_rush == null)
        {
            return;
        }

        int currentAbsorbCount = StatisticsManager.Instance.GetCount(StatisticsManager.GameStatisticType.EnemyAbsorb);
        int steps = Mathf.Max(0, currentAbsorbCount - _baselineAbsorbCount) / Mathf.Max(1, _absorptionsPerStep);
        float bonusPercent = Mathf.Min(steps * _distancePercentPerStep, MAX_BONUS_PERCENT);

        _rush.SetEnergyConversionDistanceBonus(bonusPercent);
    }
}
