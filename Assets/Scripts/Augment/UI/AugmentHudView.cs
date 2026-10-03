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
    }
}
