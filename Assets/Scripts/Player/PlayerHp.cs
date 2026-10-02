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
    public bool isUnBeat = false;
    public int playerHP = 5;
    public int maxPlayerHP = 5;

    public float endUnBeatTime = 0;
    private float currentTime = 0;

    public Coroutine unbeatRoutine;
    [Header("플레이어 무적 상태 표시 관련")]
    [SerializeField] MeshRenderer playerMeshRenderer;
    [Tooltip("플레이어가 무적 상태일 때 적용할 머티리얼 (0: 기본, 1: 무적상태)")]
    public List<Material> playerMaterials = new List<Material>();
    private float blinkDuration = 0.1f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        hpText.text = playerHP + " / 5";

        volume = FindFirstObjectByType<Volume>();

        if (volume.profile.TryGet<Vignette>(out var tmpVignette))
        {
            vignette = tmpVignette;
        }
        attackedVignetteIntensity = 0.35f;
        SetVignetteIntensity(0.0f);
    }

    public IEnumerator UnBeatTime(float sec)
    {
        //무적 상태 이펙트 보여주기
        playerMeshRenderer.material = playerMaterials[1];
        isUnBeat = true;
        currentTime = 0f;
        while (currentTime < sec)
        {
            currentTime += Time.deltaTime;
            yield return null;
        }
        SetVignetteIntensity(0.0f);
        playerMeshRenderer.material = playerMaterials[0];
        isUnBeat = false;
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
        else
        {
            if (unbeatRoutine != null)
                StopCoroutine(unbeatRoutine);
            //무적 시간 처리
            unbeatRoutine = StartCoroutine(UnBeatTime(2f));
        }
    }

    public void UpdateUnBeatTime(float time)
    {
        endUnBeatTime = Mathf.Max(endUnBeatTime, currentTime + time);
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
        if (other.CompareTag("HealPack") && playerHP < 5)
        {
            playerHP += 1;
            gameManager.PlayerAttackedUI(playerHP);
            Destroy(other.gameObject);
        }
    }

}
