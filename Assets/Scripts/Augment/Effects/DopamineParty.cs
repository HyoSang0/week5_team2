using UnityEngine;

/// <summary>
/// 도파민 파티 증강 효과. 증강 선택 이후 얻은 점수가 _goalScore에 도달할 때마다 흡수 쿨타임을 줄인다.
/// </summary>
[CreateAssetMenu(menuName = "Augment/Effects/Dopamine Party")]
public class DopamineParty : AugmentEffect
{
    [Header("설정값")]
    [SerializeField] private float _goalScore = 100;
    [SerializeField] private float _cooldownReductionFlat = 0.3f;

    // 증강 선택 이후 누적된 점수
    private float _currentScoreCount = 0;
    private AbsortionAbility_Sejin _absrobSkill;

    /// <summary>
    /// 증강 적용 시 플레이어 흡수 컴포넌트를 찾아 보관한다.
    /// </summary>
    public override void OnApply()
    {
        _absrobSkill = FindFirstObjectByType<AbsortionAbility_Sejin>();
    }

    /// <summary>
    /// 증가한 점수 score를 누적하고, _goalScore를 채울 때마다 흡수 쿨타임을 _cooldownReductionFlat초 줄인다.
    /// 한 번에 여러 목표치를 넘으면 넘은 횟수만큼 줄이며, 남은 점수는 다음 누적에 이월한다.
    /// </summary>
    public override void OnScoreIncreased(float score)
    {
        if (_goalScore <= 0f || _absrobSkill == null)
        {
            return;
        }

        _currentScoreCount += score;

        while (_currentScoreCount >= _goalScore)
        {
            _currentScoreCount -= _goalScore;
            _absrobSkill.ReduceCooldown(_cooldownReductionFlat);
        }
    }

    /// <summary>
    /// 새 판 시작 시 누적 점수와 캐시한 흡수 컴포넌트 참조를 비운다.
    /// </summary>
    public override void OnRunReset()
    {
        _currentScoreCount = 0.0f;
        _absrobSkill = null;
    }
}
