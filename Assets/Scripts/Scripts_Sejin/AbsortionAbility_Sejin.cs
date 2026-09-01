using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;
using TMPro;

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

    private IEnumerator ConsumeStamina()
    {
        while(isStartAbsortion)
        {
            stamina -= remainingStamina * Time.deltaTime;
            if(stamina < 0)
            {
                stamina = 0;
                StopAbility();
            }
            RefreshUI();
            yield return null;
        }
    }

    private void StartAbility()
    {
        AbsortionArea.SetActive(true);
        isStartAbsortion = true;
        StartCoroutine(ConsumeStamina());
        StopCoroutine(RegenStamina());
    }
    private void StopAbility()
    {
        AbsortionArea.SetActive(false);
        isStartAbsortion = false;
        StartCoroutine(RegenStamina());
        StopCoroutine(ConsumeStamina());
    }

    private void RefreshUI()
    {
        absortEnergyText.text = $"Absort : {stamina}";
    }

    private IEnumerator RegenStamina()
    {
        while (!isStartAbsortion)
        {
            stamina += regenStamina * Time.deltaTime;
            if(stamina > maxStamina)
            {
                stamina = maxStamina;
            }
            RefreshUI();
            yield return null;
        }
    }
}
