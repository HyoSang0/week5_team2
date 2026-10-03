using UnityEngine;

/// <summary>
/// 단일 스탯에 대한 퍼센트/플랫 변경량을 담는 직렬화 가능한 구조체.
/// percent는 가산 퍼센트(20 = +20%), flat는 절대 추가값이며 0이면 변화 없음.
/// </summary>
[System.Serializable]
public struct StatModifier
{
    [Header("Stat Modifier")]
    [SerializeField] private StatType _stat;
    [SerializeField] private float _percent;
    [SerializeField] private float _flat;

    /// <summary>
    /// 이 수정자가 적용될 스탯 유형을 반환한다.
    /// </summary>
    public StatType Stat => _stat;

    /// <summary>
    /// 가산 퍼센트 변경값을 반환한다(20이면 +20%).
    /// </summary>
    public float Percent => _percent;

    /// <summary>
    /// 플랫 변경값을 반환한다.
    /// </summary>
    public float Flat => _flat;

    /// <summary>
    /// 지정된 스탯에 적용할 퍼센트/플랫 값으로 수정자를 생성한다.
    /// stat, percent, flat를 입력받아 동일 값을 반환 프로퍼티에 노출한다.
    /// </summary>
    public StatModifier(StatType stat, float percent, float flat)
    {
        _stat = stat;
        _percent = percent;
        _flat = flat;
    }
}
