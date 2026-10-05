using UnityEngine;

[CreateAssetMenu(menuName = "Augment/Effects/Dopamine Party")]
public class DopamineParty : AugmentEffect
{
    [SerializeField] private float _goalScore = 100;
    [SerializeField] private float _cooldownReductionFlat = 0.3f;
    private float _currentScoreCount = 0;
    private AbsortionAbility_Sejin _absrobSkill;

    public override void OnApply()
    {
        _absrobSkill = FindFirstObjectByType<AbsortionAbility_Sejin>();
    }

    /// <summary>
    /// 적이 흡수 처리
    /// </summary>
    public override void OnScoreIncreased(float score)
    {
        _currentScoreCount += score;
        Debug.Log($"현재 스택 {_currentScoreCount}");

        //100마리를 채웠다면 최대 체력 증가
        if (_currentScoreCount >= _goalScore)
        {
            _currentScoreCount -= _goalScore;
            //흡수 쿨타임 감소시키기
            _absrobSkill.ReduceCooldown(_cooldownReductionFlat);
        }
    }

    public override void OnRunReset()
    {
        _currentScoreCount = 0.0f;
    }
}
