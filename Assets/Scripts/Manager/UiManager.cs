using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class UiManager : MonoBehaviour
{
    [Header("HUD")]
    [SerializeField] private Slider hpSlider;
    [SerializeField] private Slider _experienceSlider;

    [Header("Text")]
    [SerializeField] private TextMeshProUGUI hpText;
    [SerializeField] private TextMeshProUGUI _experienceText;

    [Header("HP Follow")]
    [SerializeField] private Vector2 _hpScreenOffset = new Vector2(0f, -50f);
    private RectTransform _hpUiRoot;
    private RectTransform _canvasRect;
    private Camera _mainCamera;

    [Header("Reference")]
    [FormerlySerializedAs("pHp")]
    [SerializeField] private PlayerHp _playerHp;
    private GameManager _gameManager;

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
        _gameManager = GameManager.Instance;
        _playerHp.OnHpChanged += UpdateHud;
        _gameManager.ExperienceChanged += UpdateExperienceHud;

        UpdateHud();
        UpdateExperienceHud();
    }

    private void OnDestroy()
    {
        _playerHp.OnHpChanged -= UpdateHud;
        _gameManager.ExperienceChanged -= UpdateExperienceHud;
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

    /// <summary>
    /// 현재 레벨 경험치와 다음 레벨 요구치를 경험치 바와 텍스트에 표시한다.
    /// GameManager의 Level, CurrentLevelExperience, NextLevelRequirement를 읽어 UI 상태를 변경한다.
    /// </summary>
    private void UpdateExperienceHud()
    {
        int current = _gameManager.CurrentLevelExperience;
        int required = _gameManager.NextLevelRequirement;

        _experienceSlider.minValue = 0;
        _experienceSlider.maxValue = required;
        _experienceSlider.value = current;
        _experienceText.text = $"Lv.{_gameManager.Level}  {current}/{required}";
    }
}
