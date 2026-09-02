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
        aeText.text = string.Format("AE : {0:F0} / 30", absorb.stamina);
        deText.text = string.Format("DE : {0:F0} / 30", rush.energy);
        hpSlider.value = (float)pHp.playerHP / pHp.maxPlayerHP;
        aeSlider.value = absorb.stamina / absorb.maxStamina;
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
            Debug.Log(pHp.playerHP + " / " + pHp.maxPlayerHP);
        }
    }
}
