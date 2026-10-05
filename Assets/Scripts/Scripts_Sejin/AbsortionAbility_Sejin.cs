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
    private float _lastCooldownSeconds;

    [Header("Conversion")]
    [SerializeField, Min(0f)] private float _conversionActiveSeconds = 1.5f;
    [SerializeField, Min(0f)] private float _conversionCooldownSeconds = 5f;

    public bool isStartAbsortion = false;

    // 증강 스탯 재계산에 사용할 쿨타임 기준값
    private float _baseCooldownSeconds;

    // 전환(시간제) 쿨타임의 증강 재계산용 기준값
    private float _baseConversionCooldownSeconds;

    private AbsortionArea_Sejin _activeArea;

    // 증강 선택으로 바뀐 현재 흡수 영역 유형
    private AbsorptionAreaType _selectedAreaType = AbsorptionAreaType.None;

    // 이번 활성화가 시간제(전환) 모드인지 기록한다
    private bool _isTimedActivation;

    // 시간제 활성의 종료 시각
    private float _timedEndTime;

    /// <summary>현재 선택된 흡수 영역이 시간제(전환) 모드인지 반환한다.</summary>
    public bool IsTimedMode => _selectedAreaType == AbsorptionAreaType.Conversion;

    /// <summary>시간제(전환) 흡수가 현재 활성 상태인지 반환한다.</summary>
    public bool IsTimedActive => _isTimedActivation && isStartAbsortion;

    public event Action OnAbsorbStarted;
    public event Action OnAbsorbEnded;

    /// <summary>
    /// 일반 흡수 또는 시간제 포식이 활성 상태인지 반환한다.
    /// isStartAbsortion을 읽으며 UI 활성 표시 여부를 결정한다.
    /// </summary>
    public bool IsAbsorptionActive => isStartAbsortion;

    /// <summary>
    /// 활성 흡수의 게이지 비율을 반환한다.
    /// 일반 흡수 중에는 1, 시간제 포식 중에는 남은 지속 시간을 비율로 반환한다.
    /// </summary>
    public float ActiveAbsorptionRatio
    {
        get
        {
            if (!_isTimedActivation)
            {
                return 1f;
            }

            if (_conversionActiveSeconds <= 0f)
            {
                return 0f;
            }

            return Mathf.Clamp01((_timedEndTime - Time.time) / _conversionActiveSeconds);
        }
    }

    /// <summary>
    /// 마지막 흡수 쿨타임의 경과 비율(0~1)을 반환한다.
    /// 준비 상태면 1, 쿨타임 시작 시점이면 0이며 Time.time 기준으로 계산한다.
    /// </summary>
    public float AbsorptionCooldownRatio
    {
        get
        {
            if (Time.time >= _nextAvailableTime || _lastCooldownSeconds <= 0f)
            {
                return 1f;
            }

            return Mathf.Clamp01(1f - (_nextAvailableTime - Time.time) / _lastCooldownSeconds);
        }
    }

    void Awake()
    {
        inputActions = new InputSystem_Actions();
        _baseCooldownSeconds = _cooldownSeconds;
        _baseConversionCooldownSeconds = _conversionCooldownSeconds;
        _selectedAreaType = AbsorptionAreaType.Default;
    }

    void OnEnable()
    {
        inputActions.Enable();
        AugmentEvents.OnAbsorptionAreaTypeSelected += HandleAbsorptionAreaTypeSelected;
    }

    void OnDisable()
    {
        if (isStartAbsortion)
        {
            StopAbility();
        }

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
        _conversionCooldownSeconds = PlayerStats.Instance.Apply(StatType.AbsorbCooldown, _baseConversionCooldownSeconds);
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
        // 시간제 활성은 키를 떼도 유지되며 지속 시간이 끝날 때만 닫는다.
        if (_isTimedActivation)
        {
            return;
        }

        StopAbility();
    }

    /// <summary>
    /// 쿨다운이 아니고 아직 흡수 중이 아닐 때 흡수를 시작한다.
    /// Time.time과 _nextAvailableTime을 비교해 성공 시 흡수 영역을 켜고 PlayerController에 Charge 이동 상태를 알린다.
    /// 시간제(전환) 모드면 StartTimedAbility로 분기해 이동 상태를 변경하지 않는다.
    /// </summary>
    private void StartAbility()
    {
        if (isStartAbsortion || Time.time < _nextAvailableTime)
            return;

        if (IsTimedMode)
        {
            StartTimedAbility();
            return;
        }

        // 활성화 시점의 선택 유형으로 영역 하나를 확정하고 Stop이 같은 영역을 닫도록 캐시한다.
        // 보유 중에 유형이 바뀌면 다음 활성화부터 반영된다.
        _activeArea = ResolveArea(_selectedAreaType);
        if (_activeArea != null)
        {
            _activeArea.gameObject.SetActive(true);
        }

        isStartAbsortion = true;
        _playerController.SetAbsorbState(true);

        AugmentEvents.RaiseAbsorptionStarted();
        OnAbsorbStarted?.Invoke();
    }

    /// <summary>
    /// 시간제(전환) 흡수를 시작한다. 전환 영역을 켜고 isStartAbsortion을 true로,
    /// 종료 시각을 Time.time + _conversionActiveSeconds로 기록한다.
    /// 이번 활성화가 시간제임을 _isTimedActivation에 남기고 이동 감속은 걸지 않는다.
    /// </summary>
    private void StartTimedAbility()
    {
        _isTimedActivation = true;

        _activeArea = ResolveArea(AbsorptionAreaType.Conversion);
        if (_activeArea != null)
        {
            _activeArea.gameObject.SetActive(true);
        }

        isStartAbsortion = true;
        _timedEndTime = Time.time + _conversionActiveSeconds;

        AugmentEvents.RaiseAbsorptionStarted();
        OnAbsorbStarted?.Invoke();
    }

    /// <summary>
    /// 시간제 활성 중 지속 시간이 끝났는지 확인해 흡수를 닫는다.
    /// Time.time은 일시정지 중 멈추므로 일시정지 중에는 종료되지 않는다.
    /// </summary>
    private void Update()
    {
        if (_isTimedActivation && isStartAbsortion && Time.time >= _timedEndTime)
        {
            StopAbility();
        }
    }

    /// <summary>
    /// 활성화된 흡수 영역을 끄고 다음 사용 가능 시각을 설정한다.
    /// 시간제(전환) 활성이면 전환 쿨타임을 적용하고 PlayerController의 흡수 이동 상태를 건드리지 않는다.
    /// 기본 홀드 활성이면 _cooldownSeconds를 적용하고 SetAbsorbState(false)를 호출한다.
    /// 영역 상태, isStartAbsortion, _isTimedActivation, _nextAvailableTime을 변경한다.
    /// </summary>
    private void StopAbility()
    {
        if (!isStartAbsortion)
            return;

        if (_activeArea != null)
        {
            _activeArea.gameObject.SetActive(false);
        }

        bool wasTimed = _isTimedActivation;
        float cooldownSeconds = wasTimed ? _conversionCooldownSeconds : _cooldownSeconds;

        _activeArea = null;
        isStartAbsortion = false;
        _isTimedActivation = false;

        if (!wasTimed && _playerController != null)
        {
            _playerController.SetAbsorbState(false);
        }

        _lastCooldownSeconds = cooldownSeconds;
        _nextAvailableTime = Time.time + _lastCooldownSeconds;

        // 홀드 모드의 종료 시점 일괄 흡수가 모두 집계된 뒤에 종료를 통지한다.
        AugmentEvents.RaiseAbsorptionEnded();
        _nextAvailableTime = Time.time + cooldownSeconds;
        OnAbsorbEnded?.Invoke();
    }

    /// <summary>
    /// 시간제(전환) 활성 중 접촉한 적의 즉시 흡수를 시도한다.
    /// enemy는 접촉 판정에서 얻은 Enemy이며, 시간제 활성이 아니거나 영역이 없으면 false를 반환한다.
    /// </summary>
    public bool TryContactAbsorb(Enemy enemy)
    {
        if (!IsTimedActive || _activeArea == null)
        {
            return false;
        }

        return _activeArea.TryAbsorbOnContact(enemy);
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

    // 돌진이 시작될 때 활성화된 흡수를 종료하고 기존 쿨타임을 시작한다.
    public void InterruptForRush()
    {
        StopAbility();
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
