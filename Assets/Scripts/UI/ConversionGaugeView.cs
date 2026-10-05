using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 전환(시간제) 흡수의 지속 시간과 쿨타임을 슬라이더 게이지로 표시하는 뷰.
/// 전환 증강이 없으면 게이지를 숨기고, 있으면 활성 중에는 남은 시간을 Accent 색으로,
/// 쿨타임 중에는 진행률을 SurfaceNavy 색으로, 준비 상태에서는 가득 찬 Accent 게이지를 보여준다.
/// </summary>
public class ConversionGaugeView : MonoBehaviour
{
    [Header("Conversion Gauge")]
    [SerializeField] private AbsortionAbility_Sejin _ability;
    [SerializeField] private Slider _slider;
    [SerializeField] private Image _fill;

    /// <summary>
    /// 전환 흡수 능력의 상태를 매 프레임 게이지에 반영한다.
    /// _ability의 IsTimedMode/IsTimedActive/TimedActiveRatio/TimedCooldownRatio를 읽어
    /// _slider 오브젝트의 활성 여부와 value, _fill의 색을 변경한다.
    /// </summary>
    private void Update()
    {
        // 전환 증강이 없으면 게이지를 숨긴 상태로 유지한다.
        if (!_ability.IsTimedMode)
        {
            if (_slider.gameObject.activeSelf)
            {
                _slider.gameObject.SetActive(false);
            }
            return;
        }

        if (!_slider.gameObject.activeSelf)
        {
            _slider.gameObject.SetActive(true);
        }

        // 포식 모드 활성 중에는 남은 지속 시간을 강조색으로 표시한다.
        if (_ability.IsTimedActive)
        {
            _slider.value = _ability.TimedActiveRatio;
            _fill.color = UIPalette.Accent;
            return;
        }

        // 쿨타임 중에는 진행률을 남색으로, 준비되면 가득 찬 강조색 게이지를 표시한다.
        if (_ability.TimedCooldownRatio < 1f)
        {
            _slider.value = _ability.TimedCooldownRatio;
            _fill.color = UIPalette.SurfaceNavy;
            return;
        }

        _slider.value = 1f;
        _fill.color = UIPalette.Accent;
    }
}
