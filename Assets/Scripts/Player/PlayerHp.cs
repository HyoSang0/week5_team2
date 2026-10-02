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
        endUnBeatTime = currentTime + time;
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
        if (other.CompareTag("HealPack") && playerHP < 5)
        {
            playerHP += 1;
            gameManager.PlayerAttackedUI(playerHP);
            Destroy(other.gameObject);
        }
    }

}
