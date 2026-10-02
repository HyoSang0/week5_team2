using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;


public class PlayerHp : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI hpText;
    Rigidbody rb;
    [SerializeField] GameManager gameManager;
    [SerializeField] private Volume volume;
    [SerializeField] DarkVignette darkVignette;
    [SerializeField] private float attackedVignetteIntensity = 0.35f;
    private Vignette vignette;
    public int playerHP = 5;
    public int maxPlayerHP = 5;

    [Header("플레이어 무적 상태 표시 관련")]
    public bool isUnBeatHit = false;
    public bool isUnBeatDash = false;
    public float endUnBeatTimeHit = 0;
    private float currentTimeHit = 0;

    public Coroutine unbeatRoutineHit;
    public Coroutine unbeatRoutineDash;
    [SerializeField] MeshRenderer playerMeshRenderer;
    [Tooltip("플레이어가 무적 상태일 때 적용할 머티리얼 (0: 기본, 1: 피격 무적)")]
    public List<Material> playerMaterials = new List<Material>();
    public GameObject dashShield;

    // 증강 스탯 재계산에 사용할 maxPlayerHP의 기준값
    private int _baseMaxPlayerHP;

    void Awake()
    {
        _baseMaxPlayerHP = maxPlayerHP;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        dashShield.SetActive(false);
        rb = GetComponent<Rigidbody>();
        hpText.text = playerHP + " / " + maxPlayerHP;

        // Start는 씬의 모든 Awake 이후 실행되므로 여기서 구독하면 PlayerStats.Awake 순서와 무관하다.
        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.OnStatsChanged += ApplyAugmentStats;
        }

        ApplyAugmentStats();

        volume = FindFirstObjectByType<Volume>();

        if (volume.profile.TryGet<Vignette>(out var tmpVignette))
        {
            vignette = tmpVignette;
        }
        attackedVignetteIntensity = 0.35f;
        SetVignetteIntensity(0.0f);
    }

    private void OnDestroy()
    {
        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.OnStatsChanged -= ApplyAugmentStats;
        }
    }

    /// <summary>
    /// PlayerStats의 MaxHp 증강을 기준값에 적용해 maxPlayerHP를 재계산한다.
    /// 최대치가 늘면 늘어난 만큼 playerHP를 올리고, 줄면 playerHP를 최대치로 clamp한다.
    /// 변경된 값은 hpText와 gameManager UI에 반영한다.
    /// </summary>
    private void ApplyAugmentStats()
    {
        if (PlayerStats.Instance == null)
        {
            return;
        }

        int newMax = Mathf.Max(1, PlayerStats.Instance.ApplyInt(StatType.MaxHp, _baseMaxPlayerHP));

        if (newMax > maxPlayerHP)
        {
            playerHP += newMax - maxPlayerHP;
        }
        maxPlayerHP = newMax;
        playerHP = Mathf.Min(playerHP, maxPlayerHP);

        hpText.text = playerHP + " / " + maxPlayerHP;
        gameManager.PlayerAttackedUI(playerHP);
    }

    /// <summary>
    /// amount만큼 플레이어 HP를 회복한다. playerHP는 maxPlayerHP를 초과하지 않는다.
    /// 변경된 playerHP를 gameManager.PlayerAttackedUI로 UI에 반영한다.
    /// </summary>
    public void Heal(int amount)
    {
        playerHP = Mathf.Min(playerHP + amount, maxPlayerHP);
        gameManager.PlayerAttackedUI(playerHP);
    }

    public IEnumerator UnBeatTimeForHit(float sec)
    {
        //무적 상태 이펙트 보여주기
        playerMeshRenderer.material = playerMaterials[1];
        currentTimeHit = 0f;
        while (currentTimeHit < sec)
        {
            currentTimeHit += Time.deltaTime;
            yield return null;
        }
        SetVignetteIntensity(0.0f);
        playerMeshRenderer.material = playerMaterials[0];
        isUnBeatHit = false;
    }

    public IEnumerator UnBeatTimeForDash(float sec)
    {
        dashShield.SetActive(true);
        yield return new WaitForSeconds(sec);
        dashShield.SetActive(false);
        isUnBeatDash = false; ;
    }

    public void PlayerAttacked(int damage)
    {
        //플레이어 체력 감소 처리
        playerHP -= damage;
        //체력에 따라 시야 vignette 어둡기 처리
        darkVignette.UpdateVignetteDarkness(playerHP, maxPlayerHP);
        //피격 vignette 처리
        SetVignetteIntensity(attackedVignetteIntensity);
        // UI 처리
        gameManager.PlayerAttackedUI(playerHP);

        // 사망 처리
        if (playerHP <= 0)
        {
            gameManager.PlayerDie();
        }
    }

    public void UpdateUnBeatTime(float time, bool isHit)
    {
        if (isHit)
        {
            isUnBeatHit = true;
            unbeatRoutineHit = StartCoroutine(UnBeatTimeForHit(time));
            endUnBeatTimeHit = Mathf.Max(endUnBeatTimeHit, currentTimeHit + time);
        }
        else
        {
            isUnBeatDash = true;
            unbeatRoutineDash = StartCoroutine(UnBeatTimeForDash(time));
        }
    }

    private void SetVignetteIntensity(float intensity)
    {
        vignette.intensity.overrideState = true;
        vignette.intensity.value = intensity;
    }

    void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.CompareTag("Enemy") || collision.gameObject.CompareTag("NoAbsortEnemy"))
        {
            Debug.Log("Player Attacked");
            if (!isUnBeatHit && !isUnBeatDash)
            {
                PlayerAttacked(1);
                UpdateUnBeatTime(2f, true);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("HealPack") && playerHP < maxPlayerHP)
        {
            playerHP += 1;
            gameManager.PlayerAttackedUI(playerHP);
            Destroy(other.gameObject);
        }
    }

}
