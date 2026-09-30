using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

// 마우스 우클릭 한 번으로 작동하는 흡수 능력
public class AbsortionAbility_Sejin : MonoBehaviour
{
    public TextMeshProUGUI absortEnergyText;
    public GameObject AbsortionArea;
    public float stamina = 30f;
    public float staminaCost = 1f;
    public float areaDuration = 0.1f;
    public float regenStamina = 10f;
    public float maxStamina = 30f;
    public bool isStartAbsortion = false;

    private InputSystem_Actions inputActions;
    private AbsortionArea_Sejin absorptionArea;
    private float areaEndTime;

    void Awake()
    {
        inputActions = new InputSystem_Actions();
        stamina = maxStamina;
        absorptionArea = AbsortionArea.GetComponentInChildren<AbsortionArea_Sejin>(true);
        inputActions.Player.Ability_Sejin.started += ActiveAbility;
        AbsortionArea.SetActive(false);
    }

    void OnEnable()
    {
        inputActions.Enable();
    }

    void OnDisable()
    {
        inputActions.Disable();
        StopAbility();
    }

    void OnDestroy()
    {
        inputActions.Player.Ability_Sejin.started -= ActiveAbility;
        inputActions.Dispose();
    }

    void Start()
    {
        RefreshUI();
    }

    private void ActiveAbility(InputAction.CallbackContext context)
    {
        if (isStartAbsortion || stamina < staminaCost)
            return;

        stamina -= staminaCost;
        isStartAbsortion = true;
        areaEndTime = Time.time + areaDuration;
        AbsortionArea.SetActive(true);
        absorptionArea.AbsorbNearestOnce();
        RefreshUI();
    }

    private void StopAbility()
    {
        AbsortionArea.SetActive(false);
        isStartAbsortion = false;
    }

    private void Update()
    {
        if (isStartAbsortion)
        {
            if (Time.time >= areaEndTime)
                StopAbility();
        }
        else
        {
            stamina = Mathf.Min(maxStamina, stamina + regenStamina * Time.deltaTime);
        }
        RefreshUI();
    }

    private void RefreshUI()
    {
        if (absortEnergyText != null)
            absortEnergyText.text = $"Absort : {stamina}";
    }
}
