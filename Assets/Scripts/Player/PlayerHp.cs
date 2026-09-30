using System.Collections;
using TMPro;
using UnityEngine;


public class PlayerHp : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI hpText;
    Rigidbody rb;
    [SerializeField] GameManager gameManager;
    public bool isUnBeat = false;
    public int playerHP = 5;
    public int maxPlayerHP = 5;

    public float endUnBeatTime = 0;
    private float currentTime = 0;

    public Coroutine unbeatRoutine;
    private AbsortionAbility_Sejin absorb;

    private void Awake()
    {
        absorb = GetComponent<AbsortionAbility_Sejin>();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        hpText.text = playerHP + " / 5";
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
        if(currentTime < endUnBeatTime)
        {
            isUnBeat = true;
        }
        else
        {
            isUnBeat = false;
        }
    }

    void OnCollisionStay(Collision collision)
    {
        if (!collision.gameObject.CompareTag("Enemy") &&
            !collision.gameObject.CompareTag("NoAbsortEnemy"))
            return;

        Enemy enemy = collision.gameObject.GetComponentInParent<Enemy>();

        if (enemy != null && enemy.isDead)
            return;

        if (absorb != null &&
            absorb.State == AbsortionAbility_Sejin.AbsorptionState.Active &&
            enemy != null &&
            enemy.TryAbsorb())
            return;

        if (isUnBeat)
            return;

        isUnBeat = true;
        PlayerAttacked(1);
        UpdateUnBeatTime(2f);
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
