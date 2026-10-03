using UnityEngine;
using UnityEngine.InputSystem;

// 마우스 우클릭을 누르는 동안 흡수 영역을 활성화한다.
public class AbsortionAbility_Sejin : MonoBehaviour
{
    // public TextMeshProUGUI absortEnergyText;

    private InputSystem_Actions inputActions;
    public GameObject AbsortionArea;

    // public float stamina = 30f;
    // public float remainingStamina = 10.0f;
    // public float regenStamina = 10.0f;
    // public float maxStamina = 30f;
    // [Tooltip("흡수 능력을 활성화하는데 필요한 비용")]
    // public float activateStamina;

    [Header("Cooldown")]
    [SerializeField, Min(0f)] private float _cooldownSeconds = 0f;
    private float _nextAvailableTime;

    public bool isStartAbsortion = false;

    // 증강 스탯 재계산에 사용할 쿨타임 기준값
    private float _baseCooldownSeconds;

    void Awake()
    {
        inputActions = new InputSystem_Actions();
        _baseCooldownSeconds = _cooldownSeconds;
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
    /// PlayerStats의 AbsorbCooldown 증강을 기준값에 적용해 _cooldownSeconds를 재계산한다.
    /// PlayerStats.Instance가 없으면 아무것도 하지 않는다.
    /// </summary>
    private void ApplyAugmentStats()
    {
        if (PlayerStats.Instance == null)
        {
            return;
        }

        _cooldownSeconds = PlayerStats.Instance.Apply(StatType.AbsorbCooldown, _baseCooldownSeconds);
    }

    // 입력 시작 이벤트를 받아 흡수 활성화를 시도한다.
    // context는 입력 이벤트이며 활성 상태가 변경될 수 있다.
    private void ActiveAbility(InputAction.CallbackContext context)
    {
        if (GamePause.IsPaused)
        {
            return;
        }

        StartAbility();
    }

    // 입력 종료 이벤트를 받아 활성화된 흡수를 끝낸다.
    // context는 입력 이벤트이며 흡수 상태와 쿨타임이 변경될 수 있다.
    private void DeActiveAbility(InputAction.CallbackContext context)
    {
        StopAbility();
    }

    // 현재 시각과 쿨타임 종료 시각을 비교해 흡수 영역을 켠다.
    // 활성 상태를 isStartAbsortion에 저장한다.
    private void StartAbility()
    {
        // if (stamina < activateStamina)
        //     return;
        // stamina -= activateStamina;

        if (isStartAbsortion || Time.time < _nextAvailableTime)
            return;

        AbsortionArea.SetActive(true);
        isStartAbsortion = true;
    }

    // 활성화된 흡수 영역을 끄고 다음 사용 가능 시각을 설정한다.
    // _cooldownSeconds를 사용하며 영역 상태와 _nextAvailableTime을 변경한다.
    private void StopAbility()
    {
        if (!isStartAbsortion)
            return;

        AbsortionArea.SetActive(false);
        isStartAbsortion = false;
        _nextAvailableTime = Time.time + _cooldownSeconds;
    }

    // private void RefreshUI()
    // {
    //     absortEnergyText.text = $"Absort : {stamina}";
    // }

    // private void Update()
    // {
    //     if (isStartAbsortion)
    //         HandleConsum();
    //     else
    //         HandleRegenStamina();
    //     RefreshUI();
    // }

    // private void HandleConsum()
    // {
    //     stamina -= remainingStamina * Time.deltaTime;
    //     if (stamina < 0)
    //     {
    //         stamina = 0;
    //         StopAbility();
    //     }
    // }

    // private void HandleRegenStamina()
    // {
    //     stamina += regenStamina * Time.deltaTime;
    //     if (stamina > maxStamina)
    //     {
    //         stamina = maxStamina;
    //     }
    // }
}