using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using TMPro;

/// <summary>
/// Selectable의 선택/해제 이벤트를 받아 _scaleTarget을 확대하거나 원래 크기로 되돌리는 강조 컴포넌트.
/// Awake에서 같은 오브젝트의 Selectable 색상을 UIPalette 공용 ColorBlock으로 통일하고,
/// targetGraphic은 흰색으로 둬 실제 버튼 색을 ColorBlock이 결정하며 자식 라벨 색도 함께 관리한다.
/// </summary>
[RequireComponent(typeof(Selectable))]
public class UISelectionHighlight : MonoBehaviour, ISelectHandler, IDeselectHandler
{
    [Header("Selection Highlight")]
    [SerializeField] private Transform _scaleTarget;
    [SerializeField] private float _selectedScale = 1.05f;

    private Vector3 _baseScale;
    private TMP_Text[] _labels;

    void Awake()
    {
        if (_scaleTarget == null)
        {
            _scaleTarget = transform;
        }

        _baseScale = _scaleTarget.localScale;

        Selectable selectable = GetComponent<Selectable>();
        selectable.colors = UIPalette.CreateButtonColors();

        // ColorBlock 색은 targetGraphic 색과 곱해지므로 실제 색을 결정하려면 흰색이어야 한다.
        if (selectable.targetGraphic != null)
        {
            selectable.targetGraphic.color = Color.white;
        }

        _labels = GetComponentsInChildren<TMP_Text>(true);
    }

    public void OnSelect(BaseEventData eventData)
    {
        _scaleTarget.localScale = _baseScale * _selectedScale;
        SetLabelColor(UIPalette.TextOnAccent);
    }

    public void OnDeselect(BaseEventData eventData)
    {
        _scaleTarget.localScale = _baseScale;
        SetLabelColor(UIPalette.TextPrimary);
    }

    void OnDisable()
    {
        _scaleTarget.localScale = _baseScale;
        SetLabelColor(UIPalette.TextPrimary);
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
