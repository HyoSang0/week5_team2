using UnityEngine;
using UnityEngine.UI;

using TMPro;

/// <summary>
/// 증강 선택 카드 한 장(선택/리롤 버튼, 등급·이름·설명 텍스트)을 표시하는 뷰.
/// 클릭 입력은 바인딩된 IAugmentCardHandler에게만 전달하고 판단 로직을 갖지 않는다.
/// </summary>
public class AugmentCardView : MonoBehaviour
{
    [Header("Card")]
    [SerializeField] private Button _selectButton;
    [SerializeField] private Button _rerollButton;
    [SerializeField] private TextMeshProUGUI _tierText;
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private TextMeshProUGUI _descriptionText;

    private IAugmentCardHandler _handler;

    /// <summary>
    /// 카드 번호 index와 입력 처리자 handler를 바인딩하고 버튼 리스너를 한 번만 등록한다.
    /// 반환값은 없고 이후 클릭은 handler의 HandleCardSelected/HandleCardRerolled로 위임된다.
    /// </summary>
    public void Bind(int index, IAugmentCardHandler handler)
    {
        _handler = handler;

        int cardIndex = index;
        _selectButton.onClick.RemoveAllListeners();
        _selectButton.onClick.AddListener(() => _handler?.HandleCardSelected(cardIndex));

        _rerollButton.onClick.RemoveAllListeners();
        _rerollButton.onClick.AddListener(() => _handler?.HandleCardRerolled(cardIndex));
    }

    /// <summary>
    /// 카드에 data의 등급·이름·설명 텍스트와 AugmentTierStyle의 등급 색상를 반영한다.
    /// </summary>
    public void SetData(AugmentData data)
    {
        _tierText.text = AugmentTierStyle.GetTierName(data.Tier);
        _tierText.color = AugmentTierStyle.GetTierColor(data.Tier);
        _nameText.text = data.DisplayName;
        _descriptionText.text = data.Description;
    }

    /// <summary>
    /// 리롤 버튼의 interactable 상태를 value로 변경한다.
    /// </summary>
    public void SetRerollInteractable(bool value)
    {
        _rerollButton.interactable = value;
    }

    /// <summary>
    /// 카드 GameObject 전체의 활성 상태를 value로 변경한다.
    /// </summary>
    public void SetVisible(bool value)
    {
        gameObject.SetActive(value);
    }
}
