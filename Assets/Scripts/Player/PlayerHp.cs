using System.Collections;
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
    [SerializeField] private float attackedVignetteIntensity = 0.35f;
    private Vignette vignette;
    public bool isUnBeat = false;
    public int playerHP = 5;
    public int maxPlayerHP = 5;

    public float endUnBeatTime = 0;
    private float currentTime = 0;

    public Coroutine unbeatRoutine;

    // 증강 스탯 재계산에 사용할 maxPlayerHP의 기준값
    private int _baseMaxPlayerHP;

    void Awake()
    {
        _baseMaxPlayerHP = maxPlayerHP;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
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

    // Update is called once per frame
    public IEnumerator UnBeatTime(float sec)
    {
        isUnBeat = true;
        yield return new WaitForSeconds(sec);
        isUnBeat = false;
    }

    public void PlayerAttacked(int damage)
    {
        playerHP -= damage;
        SetVignetteIntensity(attackedVignetteIntensity);
        gameManager.PlayerAttackedUI(playerHP);
        if (playerHP <= 0) gameManager.PlayerDie();
    }

    public void UpdateUnBeatTime(float time)
    {
        endUnBeatTime = Mathf.Max(endUnBeatTime, currentTime + time);
    }

    void Update()
    {
        currentTime += Time.deltaTime;
        // endUnBeatTime가 현재 시간보다 작으면 isUnBeat = false, 그 외에는 isUnBeat = true;
        if (currentTime < endUnBeatTime)
        {
            isUnBeat = true;
        }
        else
        {
            isUnBeat = false;
            SetVignetteIntensity(0.0f);
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

            if (!isUnBeat)
            {
                isUnBeat = true;
                PlayerAttacked(1);
                UpdateUnBeatTime(2f);
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
