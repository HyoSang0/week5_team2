using UnityEngine;

/// <summary>
/// 포식 증강 효과. 적을 흡수할 때마다 돌진 에너지를 충전한다.
/// </summary>
[CreateAssetMenu(menuName = "Augment/Effects/FeastEffect")]
public class FeastEffect : AugmentEffect
{
    [SerializeField] private float _energyPerAbsorb = 3f;

    private RushAbility_Sejin _rush;

    /// <summary>
    /// 증강 적용 시신의 플레이어 돌진 컴포넌트를 찾아 보관한다.
    /// </summary>
    public override void OnApply()
    {
        _rush = FindFirstObjectByType<RushAbility_Sejin>();
    }

    /// <summary>
    /// 적이 흡수되면 enemy와 무관하게 돌진 에너지를 energyPerAbsorb만큼 충전한다.
    /// </summary>
    public override void OnEnemyAbsorbed(Enemy enemy)
    {
        if (_rush != null)
        {
            _rush.AddEnergy(_energyPerAbsorb);
        }
    }

    /// <summary>
    /// 새 판 시작 시 캐시한 돌진 컴포넌트 참조를 비운다.
    /// </summary>
    public override void OnRunReset()
    {
        _rush = null;
    }
}
