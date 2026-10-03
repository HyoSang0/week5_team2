using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class UiManager : MonoBehaviour
{
    [Header("HUD")]
    [SerializeField] private Slider hpSlider;

    [Header("Text")]
    [SerializeField] private TextMeshProUGUI hpText;

    [Header("HP Follow")]
    [SerializeField] private Vector2 _hpScreenOffset = new Vector2(0f, -50f);
    private RectTransform _hpUiRoot;
    private RectTransform _canvasRect;
    private Camera _mainCamera;

    [Header("Reference")]
    [FormerlySerializedAs("pHp")]
    [SerializeField] private PlayerHp _playerHp;

    void Awake()
    {
        _hpUiRoot = hpSlider.transform.parent.GetComponent<RectTransform>();
        _canvasRect = _hpUiRoot
            .GetComponentInParent<Canvas>()
            .rootCanvas
            .GetComponent<RectTransform>();
        _mainCamera = Camera.main;
    }

    void Start()
    {
        _playerHp.OnHpChanged += UpdateHud;
        UpdateHud();
    }

    private void OnDestroy()
    {
        _playerHp.OnHpChanged -= UpdateHud;
    }

    /// <summary>
    /// PlayerHp의 현재 HP와 최대 HP를 "체력: {0}/{1}" 텍스트와 슬라이더 값에 반영한다.
    /// PlayerHp.OnHpChanged 이벤트와 Start에서 호출된다.
    /// </summary>
    private void UpdateHud()
    {
        hpText.text = string.Format("체력: {0}/{1}", _playerHp.playerHP, _playerHp.maxPlayerHP);
        hpSlider.value = (float)_playerHp.playerHP / _playerHp.maxPlayerHP;
    }

    void LateUpdate()
    {
        Vector3 screenPosition = _mainCamera.WorldToScreenPoint(_playerHp.transform.position);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvasRect,
            (Vector2)screenPosition,
            null,
            out Vector2 localPosition);

        _hpUiRoot.anchoredPosition = localPosition + _hpScreenOffset;
    }
}
