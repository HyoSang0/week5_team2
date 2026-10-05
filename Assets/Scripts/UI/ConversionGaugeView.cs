using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 일반 흡수와 시간제 포식의 활성 상태 및 쿨타임을 슬라이더 게이지로 표시하는 뷰.
/// 준비 및 일반 흡수 중에는 가득 찬 Accent 게이지를, 포식 중에는 남은 시간을,
/// 쿨타임 중에는 Accent 색으로 경과 비율을 보여준다.
/// </summary>
public class ConversionGaugeView : MonoBehaviour
{
    [Header("Absorption Ability Gauge")]
    [SerializeField] private AbsortionAbility_Sejin _ability;
    [SerializeField] private Slider _slider;
    [SerializeField] private Image _fill;

    void Awake()
    {
        if (_ability == null)
        {
            _ability = FindFirstObjectByType<AbsortionAbility_Sejin>();
        }
    }

    /// <summary>
    /// 일반 흡수 또는 시간제 포식 능력의 상태를 매 프레임 게이지에 반영한다.
    /// _ability의 IsAbsorptionActive/ActiveAbsorptionRatio/AbsorptionCooldownRatio를 읽어
    /// _slider 오브젝트의 활성 여부와 value, _fill의 색을 변경한다.
    /// </summary>
    private void Update()
    {
        if (_ability == null)
        {
            _slider.gameObject.SetActive(false);
            return;
        }

        if (!_slider.gameObject.activeSelf)
        {
            _slider.gameObject.SetActive(true);
        }

        if (_ability.IsAbsorptionActive)
        {
            _slider.value = _ability.ActiveAbsorptionRatio;
            _fill.color = UIPalette.Accent;
            return;
        }

        if (_ability.AbsorptionCooldownRatio < 1f)
        {
            _slider.value = _ability.AbsorptionCooldownRatio;
            _fill.color = UIPalette.Accent;
            return;
        }

        _slider.value = 1f;
        _fill.color = UIPalette.Accent;
    }
}
