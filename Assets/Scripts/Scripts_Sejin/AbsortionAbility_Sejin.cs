using UnityEngine;
using UnityEngine.InputSystem;

public class AbsortionAbility_Sejin : MonoBehaviour
{
    public enum AbsorptionState
    {
        Ready,
        Active,
        Cooldown
    }

    [SerializeField] private GameObject AbsortionArea;
    [SerializeField, Min(0.01f)] private float activeDuration = 2f;
    [SerializeField, Min(0f)] private float cooldownDuration = 4f;

    public AbsorptionState State { get; private set; } = AbsorptionState.Ready;
    public float ActiveDuration => activeDuration;
    public float CooldownDuration => cooldownDuration;
    public float RemainingTime => State == AbsorptionState.Ready
        ? 0f
        : Mathf.Max(0f, stateEndTime - Time.time);

    private InputSystem_Actions inputActions;
    private float stateEndTime;

    private void Awake()
    {
        activeDuration = Mathf.Max(0.01f, activeDuration);
        cooldownDuration = Mathf.Max(0f, cooldownDuration);
        inputActions = new InputSystem_Actions();
        AbsortionArea.SetActive(false);
    }

    private void OnEnable()
    {
        inputActions.Player.Ability_Sejin.performed += OnAbsorbPressed;
        inputActions.Enable();
    }

    private void OnDisable()
    {
        inputActions.Player.Ability_Sejin.performed -= OnAbsorbPressed;
        inputActions.Disable();
        AbsortionArea.SetActive(false);
    }

    private void OnDestroy()
    {
        inputActions.Dispose();
    }

    private void OnAbsorbPressed(InputAction.CallbackContext context)
    {
        if (State != AbsorptionState.Ready)
            return;

        State = AbsorptionState.Active;
        stateEndTime = Time.time + activeDuration;
        AbsortionArea.SetActive(true);
    }

    private void Update()
    { 
        if (State == AbsorptionState.Active && Time.time >= stateEndTime)
        {
            EndAbsorptionAndStartCooldown();
        }
        else if (State == AbsorptionState.Cooldown && Time.time >= stateEndTime)
        {
            State = AbsorptionState.Ready;
        }
    }

    public void EndAbsorptionAndStartCooldown()
    {
        if (State != AbsorptionState.Active)
            return;

        AbsortionArea.SetActive(false);
        State = AbsorptionState.Cooldown;
        stateEndTime = Time.time + cooldownDuration;
    }

#if false // 이전 홀드형 흡수. 현재는 시간제 능력과 쿨다운으로 대체했다.
    public float stamina = 30f;
    public float remainingStamina = 10f;
    public float regenStamina = 10f;
    public float maxStamina = 30f;
    public bool isStartAbsortion;

    private void DeActiveAbility(InputAction.CallbackContext context)
    {
        StopAbility();
    }

    private void StartAbility()
    {
        AbsortionArea.SetActive(true);
        isStartAbsortion = true;
    }

    private void StopAbility()
    {
        AbsortionArea.SetActive(false);
        isStartAbsortion = false;
    }

    private void HandleConsum()
    {
        stamina -= remainingStamina * Time.deltaTime;
        if (stamina < 0f)
        {
            stamina = 0f;
            StopAbility();
        }
    }

    private void HandleRegenStamina()
    {
        stamina = Mathf.Min(maxStamina, stamina + regenStamina * Time.deltaTime);
    }
#endif
}
