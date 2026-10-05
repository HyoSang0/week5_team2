using System;

using UnityEngine;
using UnityEngine.InputSystem;

// 마우스 우클릭을 누르는 동안 흡수 영역을 활성화한다.
public class AbsortionAbility_Sejin : MonoBehaviour
{
    // public TextMeshProUGUI absortEnergyText;

    private InputSystem_Actions inputActions;
    private PlayerController _playerController;

    [Header("Absorption Area Binding")]
    [SerializeField] private AbsorptionAreaBinding[] _areaBindings;

    [Header("Cooldown")]
    [SerializeField, Min(0f)] private float _cooldownSeconds;
    private float _nextAvailableTime;

    public bool isStartAbsortion = false;

    // 증강 스탯 재계산에 사용할 쿨타임 기준값
    private float _baseCooldownSeconds;

    private AbsortionArea_Sejin _activeArea;

    // 증강 선택으로 바뀐 현재 흡수 영역 유형
    private AbsorptionAreaType _selectedAreaType = AbsorptionAreaType.None;

    void Awake()
    {
        inputActions = new InputSystem_Actions();
        _baseCooldownSeconds = _cooldownSeconds;
        _selectedAreaType = AbsorptionAreaType.Default;
    }

    void OnEnable()
    {
        inputActions.Enable();
        AugmentEvents.OnAbsorptionAreaTypeSelected += HandleAbsorptionAreaTypeSelected;
    }

    void OnDisable()
    {
        inputActions.Disable();
        AugmentEvents.OnAbsorptionAreaTypeSelected -= HandleAbsorptionAreaTypeSelected;
    }

    void Start()
    {
        _playerController = GetComponent<PlayerController>();
        inputActions.Player.Ability_Sejin.started += ActiveAbility;
        inputActions.Player.Ability_Sejin.canceled += DeActiveAbility;

        DeactivateAllAreas();

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

    /// <summary>
    /// 쿨다운이 아니고 아직 흡수 중이 아닐 때 흡수를 시작한다.
    /// Time.time과 _nextAvailableTime을 비교해 성공 시 흡수 영역을 켜고 PlayerController에 Charge 이동 상태를 알린다.
    /// </summary>
    private void StartAbility()
    {
        if (isStartAbsortion || Time.time < _nextAvailableTime)
            return;

        // 활성화 시점의 선택 유형으로 영역 하나를 확정하고 Stop이 같은 영역을 닫도록 캐시한다.
        // 보유 중에 유형이 바뀌면 다음 활성화부터 반영된다.
        _activeArea = ResolveArea(_selectedAreaType);
        if (_activeArea != null)
        {
            _activeArea.gameObject.SetActive(true);
        }

        isStartAbsortion = true;
        _playerController.SetAbsorbState(true);
    }

    /// <summary>
    /// 활성화된 흡수 영역을 끄고 PlayerController의 흡수 이동 상태를 해제한 뒤 다음 사용 가능 시각을 설정한다.
    /// _cooldownSeconds를 사용하며 영역 상태, isStartAbsortion, _nextAvailableTime을 변경한다.
    /// </summary>
    private void StopAbility()
    {
        if (!isStartAbsortion)
            return;

        if (_activeArea != null)
        {
            _activeArea.gameObject.SetActive(false);
        }

        _activeArea = null;
        isStartAbsortion = false;
        _playerController.SetAbsorbState(false);
        _nextAvailableTime = Time.time + _cooldownSeconds;
    }

    /// <summary>
    /// AugmentEvents의 흡수 영역 유형 선택 통지를 받아 _selectedAreaType를 갱신한다.
    /// type은 증강에서 선택한 유형이며 None은 통지되지 않는다.
    /// </summary>
    private void HandleAbsorptionAreaTypeSelected(AbsorptionAreaType type)
    {
        _selectedAreaType = type;
    }

    /// <summary>
    /// 흡수 영역 유형에 대응하는 흡수 영역 컴포넌트를 반환한다.
    /// type은 바인딩 목록에서 먼저 찾고, 대응 항목이 없으면 기본 AbsortionArea의 컴포넌트를 반환한다.
    /// </summary>
    private AbsortionArea_Sejin ResolveArea(AbsorptionAreaType type)
    {
        if (_selectedAreaType == type)
        {
            if (_activeArea != null)
                return _activeArea;

        }
        if (type == AbsorptionAreaType.Default && _areaBindings == null)
        {
            return null;
        }

        foreach (AbsorptionAreaBinding binding in _areaBindings)
        {
            if (binding != null && binding.Type == type && binding.Area != null)
            {
                return binding.Area;
            }
        }

        return null;
    }

    /// <summary>
    /// 기본 영역과 바인딩된 모든 후보 영역을 비활성화한다.
    /// Start에서 한 번 호출되어 흡수 활성화 시 영역이 하나만 존재하도록 만든다.
    /// </summary>
    private void DeactivateAllAreas()
    {
        if (_areaBindings == null)
        {
            return;
        }

        foreach (AbsorptionAreaBinding binding in _areaBindings)
        {
            if (binding != null && binding.Area != null)
            {
                binding.Area.gameObject.SetActive(false);
            }
        }
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

/// <summary>
/// 흡수 영역 유형 하나를 AbsortionArea_Sejin 컴포넌트에 연결하는 직렬화 항목.
/// </summary>
[Serializable]
public class AbsorptionAreaBinding
{
    [SerializeField] private AbsorptionAreaType _type;
    [SerializeField] private AbsortionArea_Sejin _area;

    /// <summary>
    /// 바인딩된 흡수 영역 유형을 반환한다.
    /// </summary>
    public AbsorptionAreaType Type => _type;

    /// <summary>
    /// 유형에 연결된 흡수 영역 컴포넌트를 반환한다. 설정하지 않으면 null을 반환한다.
    /// </summary>
    public AbsortionArea_Sejin Area => _area;
}
