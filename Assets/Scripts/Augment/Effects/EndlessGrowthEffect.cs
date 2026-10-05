using UnityEngine;

[CreateAssetMenu(menuName = "Augment/Effects/Endless Growth")]
public class EndlessGrowthEffect : AugmentEffect
{
    [Header("설정값")]
    [SerializeField] private int _targetAbsorbCount = 100;
    [SerializeField] private float _hpIncreaseFlat = 1f;
    // 내부 카운터 (선택 이후 측정용)
    private int _currentAbsorbCount = 0;

    private PlayerHp _playerHp;

    public override void OnApply()
    {
        _playerHp = FindFirstObjectByType<PlayerHp>();
    }

    /// <summary>
    /// 적이 흡수 처리
    /// </summary>
    public override void OnEnemyAbsorbed(Enemy enemy)
    {
        _currentAbsorbCount++;
        //Debug.Log($"현재 스택 {_currentAbsorbCount}");

        //100마리를 채웠다면 최대 체력 증가
        if (_currentAbsorbCount >= _targetAbsorbCount)
        {
            _currentAbsorbCount = 0;
            //최대 체력 증가시키기
            //Debug.Log("최대 체력 증가");
            StatModifier statModifier = new StatModifier(StatType.MaxHp, 0f, _hpIncreaseFlat);
            PlayerStats.Instance.AddModifiers(new StatModifier[] { statModifier });
        }
    }

    public override void OnRunReset()
    {
        _currentAbsorbCount = 0;
    }
}