using System.Collections.Generic;

using UnityEngine;

using TMPro;

/// <summary>
/// 보유 증강 이름을 등급 색상으로 표시하는 HUD 목록 뷰.
/// 항목 추가만 담당하고 입력이나 판단 로직은 갖지 않는다.
/// </summary>
public class AugmentHudView : MonoBehaviour
{
    [Header("HUD")]
    [SerializeField] private RectTransform _hudRoot;
    [SerializeField] private GameObject _hudEntryTemplate;

    private readonly List<GameObject> _entries = new List<GameObject>();

    /// <summary>
    /// 런타임에 생성한 보유 증강 항목만 제거하고 목록을 비운다.
    /// _hudEntryTemplate와 Inspector로 연결된 씬 오브젝트는 건드리지 않는다.
    /// </summary>
    public void ClearOwned()
    {
        for (int i = 0; i < _entries.Count; i++)
        {
            if (_entries[i] != null)
            {
                Destroy(_entries[i]);
            }
        }

        _entries.Clear();
    }

    /// <summary>
    /// 보유 증강 data를 HUD 목록 하단에 등급 색상의 이름 항목으로 추가한다.
    /// _hudEntryTemplate의 복제본을 _hudRoot 아래에 활성화한다.
    /// </summary>
    public void AddOwned(AugmentData data)
    {
        GameObject entry = Instantiate(_hudEntryTemplate, _hudRoot);
        TextMeshProUGUI text = entry.GetComponent<TextMeshProUGUI>();
        text.text = data.DisplayName;
        text.color = AugmentTierStyle.GetTierColor(data.Tier);
        entry.SetActive(true);
        _entries.Add(entry);
    }
}
