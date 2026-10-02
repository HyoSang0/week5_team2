using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

// 마우스 우클릭으로 작동하는 흡수 능력
public class AbsortionAbility_Sejin : MonoBehaviour
{
    public TextMeshProUGUI absortEnergyText;
    private InputSystem_Actions inputActions;
    public GameObject AbsortionArea;

    public float stamina = 30f;
    public float remainingStamina = 10.0f;
    public float regenStamina = 10.0f;
    public float maxStamina = 30f;
    [Tooltip("흡수 능력을 \"활성화\"하는데 필요한 비용")]
    public float activateStamina;

    public bool isStartAbsortion = false;

    // 증강 스탯 재계산에 사용할 기준값들
    private float _baseMaxStamina;
    private float _baseRegenStamina;

    void Awake()
    {
        inputActions = new InputSystem_Actions();
        stamina = maxStamina;

        _baseMaxStamina = maxStamina;
        _baseRegenStamina = regenStamina;
    }

    void OnEnable()
    {
        inputActions.Enable();
    }

    void OnDisable()
    {
        inputActions.Disable();
    }

    void Start()
    {
        inputActions.Player.Ability_Sejin.started += ActiveAbility;
        inputActions.Player.Ability_Sejin.canceled += DeActiveAbility;

        AbsortionArea.SetActive(false);

        // Start는 씬의 모든 Awake 이후 실행되므로 여기서 구독하면 PlayerStats.Awake 순서와 무관하다.
        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.OnStatsChanged += ApplyAugmentStats;
        }

        ApplyAugmentStats();
    }

    private void OnDestroy()
    {
        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.OnStatsChanged -= ApplyAugmentStats;
        }
    }

    /// <summary>
    /// PlayerStats의 MaxStamina, StaminaRegen 증강을 기준값에 적용해 maxStamina, regenStamina를 재계산한다.
    /// 줄어든 최대치보다 stamina가 크면 maxStamina로 clamp한다. PlayerStats.Instance가 없으면 아무것도 하지 않는다.
    /// </summary>
    private void ApplyAugmentStats()
    {
        if (PlayerStats.Instance == null)
        {
            return;
        }

        maxStamina = PlayerStats.Instance.Apply(StatType.MaxStamina, _baseMaxStamina);
        regenStamina = PlayerStats.Instance.Apply(StatType.StaminaRegen, _baseRegenStamina);

        if (stamina > maxStamina)
        {
            stamina = maxStamina;
        }
    }

    private void ActiveAbility(InputAction.CallbackContext context)
    {
        if (AugmentSelection.IsOpen)
        {
            return;
        }

        StartAbility();
    }

    private void DeActiveAbility(InputAction.CallbackContext context)
    {
        StopAbility();
    }

    private void StartAbility()
    {
        if (stamina < activateStamina)
            return;
        stamina -= activateStamina;
        AbsortionArea.SetActive(true);
        isStartAbsortion = true;
    }
    private void StopAbility()
    {
        AbsortionArea.SetActive(false);
        isStartAbsortion = false;
    }

    private void RefreshUI()
    {
        absortEnergyText.text = $"Absort : {stamina}";
    }

    /// <summary>
    /// amount만큼 흡수 능력의 stamina를 추가한다. stamina는 maxStamina를 초과하지 않는다.
    /// UI는 Update의 RefreshUI에서 매 프레임 갱신되므로 별도 반영 없이 stamina만 변경한다.
    /// </summary>
    public void AddStamina(float amount)
    {
        stamina += amount;
        if (stamina > maxStamina)
        {
            stamina = maxStamina;
        }
    }

    private void Update()
    {
        if (isStartAbsortion)
            HandleConsum();
        else
            HandleRegenStamina();
        RefreshUI();
    }

    private void HandleConsum()
    {
        stamina -= remainingStamina * Time.deltaTime;
        if (stamina < 0)
        {
            stamina = 0;
            StopAbility();
        }
    }

    private void HandleRegenStamina()
    {
        stamina += regenStamina * Time.deltaTime;
        if (stamina > maxStamina)
        {
            stamina = maxStamina;
        }
    }
}