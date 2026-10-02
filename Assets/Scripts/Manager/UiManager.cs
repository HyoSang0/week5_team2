using UnityEngine;
using UnityEngine.UI;

using TMPro;

public class UiManager : MonoBehaviour
{
    [Header("HUD")]
    [SerializeField] private Slider hpSlider;
    // [SerializeField] private Slider aeSlider;
    // [SerializeField] private Slider deSlider;

    [Header("Text")]
    [SerializeField] private TextMeshProUGUI hpText;
    // [SerializeField] private TextMeshProUGUI aeText;
    // [SerializeField] private TextMeshProUGUI deText;

    [Header("HP Follow")]
    [SerializeField] private Vector2 _hpScreenOffset = new Vector2(0f, -50f);
    private RectTransform _hpUiRoot;
    private RectTransform _canvasRect;
    private Camera _mainCamera;

    [Header("Reference")]
    public PlayerHp pHp;
    // public AbsortionAbility_Sejin absorb;
    // public RushAbility_Sejin rush;

    private float tempTimer = 0f;

    void Awake()
    {
        _hpUiRoot = hpSlider.transform.parent.GetComponent<RectTransform>();
        _canvasRect = _hpUiRoot
            .GetComponentInParent<Canvas>()
            .rootCanvas
            .GetComponent<RectTransform>();
        _mainCamera = Camera.main;

        UpdateHud();
    }

    // 플레이어 HP를 읽어 HP 텍스트와 슬라이더에 반영한다.
    // pHp의 현재 HP와 최대 HP를 사용하며 UI 표시 값을 변경한다.
    private void UpdateHud()
    {
        hpText.text = string.Format("HP : {0:F0} / 5", pHp.playerHP);
        hpSlider.value = (float)pHp.playerHP / pHp.maxPlayerHP;

        // aeText.text = string.Format("AE : {0:F0} / 30", absorb.stamina);
        // deText.text = string.Format("DE : {0:F0} / 30", rush.energy);
        // aeSlider.value = absorb.stamina / absorb.maxStamina;
        // deSlider.value = rush.energy / rush.maxEnergy;
    }

    void Start()
    {
    }

    void Update()
    {
        UpdateHud();

        tempTimer += Time.deltaTime;
        if (tempTimer > 2f)
        {
            tempTimer = 0f;
            // Debug.Log(pHp.playerHP + " / " + pHp.maxPlayerHP);
        }
    }

    void LateUpdate()
    {
        Vector3 screenPosition = _mainCamera.WorldToScreenPoint(pHp.transform.position);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvasRect,
            (Vector2)screenPosition,
            null,
            out Vector2 localPosition);

        _hpUiRoot.anchoredPosition = localPosition + _hpScreenOffset;
    }
}