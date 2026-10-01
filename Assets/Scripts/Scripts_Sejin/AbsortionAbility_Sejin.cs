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

    void Awake()
    {
        inputActions = new InputSystem_Actions();
        stamina = maxStamina;
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
    }

    private void ActiveAbility(InputAction.CallbackContext context)
    {
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