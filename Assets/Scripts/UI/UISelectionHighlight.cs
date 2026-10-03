using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using TMPro;

/// <summary>
/// Selectable의 선택/마우스 오버 이벤트와 interactable 변화를 받아 _scaleTarget을 확대하거나 원래 크기로 되돌리는 강조 컴포넌트.
/// Awake에서 같은 오브젝트의 Selectable을 캐시해 UIPalette 공용 ColorBlock으로 통일하고,
/// targetGraphic은 흰색으로 둬 실제 버튼 색을 ColorBlock이 결정하며 자식 라벨 색도 함께 관리한다.
/// </summary>
[RequireComponent(typeof(Selectable))]
public class UISelectionHighlight : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Selection Highlight")]
    [SerializeField] private Transform _scaleTarget;
    [SerializeField] private float _selectedScale = 1.05f;

    private Vector3 _baseScale;
    private TMP_Text[] _labels;
    private bool _selected;
    private bool _hovered;
    private Selectable _selectable;
    private bool _wasInteractable;
    private bool _isHighlighted;

    void Awake()
    {
        if (_scaleTarget == null)
        {
            _scaleTarget = transform;
        }

        _baseScale = _scaleTarget.localScale;

        _selectable = GetComponent<Selectable>();
        _selectable.colors = UIPalette.CreateButtonColors();
        _wasInteractable = _selectable.interactable;

        // ColorBlock 색은 targetGraphic 색과 곱해지므로 실제 색을 결정하려면 흰색이어야 한다.
        if (_selectable.targetGraphic != null)
        {
            _selectable.targetGraphic.color = Color.white;
        }

        _labels = GetComponentsInChildren<TMP_Text>(true);
    }

    public void OnSelect(BaseEventData eventData)
    {
        _selected = true;
        ApplyHighlight();
    }

    public void OnDeselect(BaseEventData eventData)
    {
        _selected = false;
        ApplyHighlight();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _hovered = true;
        ApplyHighlight();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _hovered = false;
        ApplyHighlight();
    }

    void OnDisable()
    {
        _selected = false;
        _hovered = false;
        ApplyHighlight();
    }

    void Update()
    {
        if (_selectable.interactable == _wasInteractable)
        {
            return;
        }

        _wasInteractable = _selectable.interactable;
        ApplyHighlight();
    }

    /// <summary>
    /// 선택/마우스 오버 상태(_selected, _hovered)와 Selectable의 interactable로 강조 여부를 결정해 반영한다.
    /// 계산한 강조 상태가 _isHighlighted와 같으면 변화가 없으므로 공유 _scaleTarget을 건드리지 않고 return 한다.
    /// 상태가 바뀔 때만 _isHighlighted를 갱신하고, 강조되면 _scaleTarget 확대와 라벨 TextOnAccent 색,
    /// 아니면 원래 크기와 라벨 TextPrimary 색을 적용한다.
    /// </summary>
    private void ApplyHighlight()
    {
        bool highlighted = (_selected || _hovered) && _selectable.interactable;

        if (highlighted == _isHighlighted)
        {
            return;
        }

        _isHighlighted = highlighted;

        if (highlighted)
        {
            _scaleTarget.localScale = _baseScale * _selectedScale;
            SetLabelColor(UIPalette.TextOnAccent);
        }
        else
        {
            _scaleTarget.localScale = _baseScale;
            SetLabelColor(UIPalette.TextPrimary);
        }
    }

    /// <summary>
    /// Awake에서 캐시한 자식 라벨들의 색을 color로 일괄 변경한다.
    /// 라벨이 없으면 아무것도 변경하지 않는다.
    /// </summary>
    private void SetLabelColor(Color color)
    {
        for (int i = 0; i < _labels.Length; i++)
        {
            _labels[i].color = color;
        }
    }
}
