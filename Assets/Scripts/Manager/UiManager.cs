using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class UiManager : MonoBehaviour
{
    
    [Header("HUD")]
    [SerializeField] Slider hpSlider;
    [SerializeField] Slider aeSlider;
    [SerializeField] Slider deSlider;

    [Header("Text")]
    [SerializeField] TextMeshProUGUI hpText;
    [SerializeField] TextMeshProUGUI aeText;
    [SerializeField] TextMeshProUGUI deText;
    

    [Header("Reference")]
    public PlayerHp pHp;
    public AbsortionAbility_Sejin absorb;
    public RushAbility_Sejin rush;

    float tempTimer = 0;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    
    void Awake()
    {
        UpdateHud();
    }

    
    void UpdateHud()
    {
        hpText.text = string.Format("HP : {0:F0} / 5", pHp.playerHP);
        if (absorb.State == AbsortionAbility_Sejin.AbsorptionState.Active)
        {
            aeText.text = string.Format("ABSORB : {0:F1}s", absorb.RemainingTime);
            aeSlider.value = absorb.RemainingTime / absorb.ActiveDuration;
        }
        else if (absorb.State == AbsortionAbility_Sejin.AbsorptionState.Cooldown)
        {
            aeText.text = string.Format("ABSORB CD : {0:F1}s", absorb.RemainingTime);
            aeSlider.value = absorb.CooldownDuration > 0f
                ? 1f - absorb.RemainingTime / absorb.CooldownDuration
                : 1f;
        }
        else
        {
            aeText.text = "ABSORB : READY";
            aeSlider.value = 1f;
        }
        deText.text = string.Format("DE : {0:F0} / 30", rush.energy);
        hpSlider.value = (float)pHp.playerHP / pHp.maxPlayerHP;
        deSlider.value = rush.energy / rush.maxEnergy;
    }

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        UpdateHud();
        tempTimer += Time.deltaTime;
        if(tempTimer > 2)
        {
            tempTimer = 0;
            // Debug.Log(pHp.playerHP + " / " + pHp.maxPlayerHP);
        }
    }
}
