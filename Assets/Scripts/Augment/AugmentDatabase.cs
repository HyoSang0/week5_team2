using System.Collections.Generic;

using UnityEngine;

/// <summary>
/// 게임에 존재하는 모든 증강 데이터를 등록하는 ScriptableObject.
/// </summary>
[CreateAssetMenu(menuName = "Augment/Augment Database")]
public class AugmentDatabase : ScriptableObject
{
    [Header("Augments")]
    [SerializeField] private List<AugmentData> _augments;

    /// <summary>
    /// 등록된 모든 증강 데이터를 읽기 전용으로 반환한다.
    /// </summary>
    public IReadOnlyList<AugmentData> Augments => _augments;

    /// <summary>
    /// 지정 등급 tier의 증강 중 이미 보유한 owned와 충돌하지 않는 후보 목록을 반환한다.
    /// 새로운 List에 담은 후보를 반환하며, owned가 비어있으면 해당 등급 전체를 반환한다.
    /// </summary>
    public List<AugmentData> GetCandidates(AugmentTier tier, IReadOnlyList<AugmentData> owned)
    {
        List<AugmentData> candidates = new List<AugmentData>();

        if (_augments == null)
        {
            return candidates;
        }

        foreach (AugmentData augment in _augments)
        {
            if (augment == null || augment.Tier != tier)
            {
                continue;
            }

            bool conflicts = false;
            if (owned != null)
            {
                foreach (AugmentData ownedAugment in owned)
                {
                    if (augment.ConflictsWith(ownedAugment))
                    {
                        conflicts = true;
                        break;
                    }
                }
            }

            if (!conflicts)
            {
                candidates.Add(augment);
            }
        }

        return candidates;
    }
}
