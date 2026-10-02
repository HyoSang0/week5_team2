using System.Collections.Generic;

using UnityEngine;

/// <summary>
/// 하나의 증강 데이터(식별자, 표시 정보, 등급, 충돌 태그, 스탯 수정자, 효과)를 담는 ScriptableObject.
/// </summary>
[CreateAssetMenu(menuName = "Augment/Augment Data")]
public class AugmentData : ScriptableObject
{
    [Header("Augment Info")]
    [SerializeField] private string _id;
    [SerializeField] private string _displayName;
    [TextArea]
    [SerializeField] private string _description;
    [SerializeField] private AugmentTier _tier;

    [Header("Conflict")]
    [SerializeField] private string[] _conflictTags;

    [Header("Effects")]
    [SerializeField] private StatModifier[] _modifiers;
    [SerializeField] private AugmentEffect _effect;

    /// <summary>
    /// 증강의 고유 식별자를 반환한다.
    /// </summary>
    public string Id => _id;

    /// <summary>
    /// UI에 표시할 이름을 반환한다.
    /// </summary>
    public string DisplayName => _displayName;

    /// <summary>
    /// 증강 설명 문구를 반환한다.
    /// </summary>
    public string Description => _description;

    /// <summary>
    /// 증강 등급을 반환한다.
    /// </summary>
    public AugmentTier Tier => _tier;

    /// <summary>
    /// 이 증강과 충돌하는 태그 목록을 읽기 전용으로 반환한다.
    /// </summary>
    public IReadOnlyList<string> ConflictTags => _conflictTags;

    /// <summary>
    /// 이 증강이 적용하는 스탯 수정자 목록을 읽기 전용으로 반환한다.
    /// </summary>
    public IReadOnlyList<StatModifier> Modifiers => _modifiers;

    /// <summary>
    /// 이 증강의 커스텀 효과 에셋을 반환한다. 없을 경우 null을 반환한다.
    /// </summary>
    public AugmentEffect Effect => _effect;

    /// <summary>
    /// 다른 증강 data와 충돌하는지 판정한다.
    /// Id가 같거나 conflictTags가 하나라도 겹치면 true를 반환한다.
    /// </summary>
    public bool ConflictsWith(AugmentData other)
    {
        if (other == null)
        {
            return false;
        }

        if (!string.IsNullOrEmpty(_id) && _id == other._id)
        {
            return true;
        }

        if (_conflictTags == null || other._conflictTags == null)
        {
            return false;
        }

        foreach (string tag in _conflictTags)
        {
            foreach (string otherTag in other._conflictTags)
            {
                if (!string.IsNullOrEmpty(tag) && tag == otherTag)
                {
                    return true;
                }
            }
        }

        return false;
    }
}
