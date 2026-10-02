using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class RushAbility_Sejin : MonoBehaviour
{
    public TextMeshProUGUI dashEnergyText;
    public float energy = 0.0f;
    public float earnEnergy = 5.0f;
    public float consumeEnergy = 10.0f;
    public float maxEnergy = 30.0f;
    public float rushSpeed = 50.0f;
    public float duringTime = 0.2f;
    public float noDamageTime = 0.7f;
    public float coolTime = 1.0f;
    public bool isDashing = false;

    private Coroutine dashRoutine;
    private Coroutine unbeatRoutine;

    private Rigidbody rb;
    private InputSystem_Actions inputActions;
    private PlayerController playerController;
    private PlayerHp playerHp;
    private GameObject dashReadyEffect;
    private Image coolDownImage;

    public UnityEvent onStartRush;
    public UnityEvent onEndRush;
    public UnityEvent onBeatable;

    public bool isRushing = false;

    private EnemyPool enemyPool;

    void Awake()
    {
        energy = 0.0f;
        inputActions = new InputSystem_Actions();
        rb = GetComponent<Rigidbody>();
        enemyPool = GameObject.Find("ObjectPool").GetComponent<EnemyPool>();
        playerHp = GetComponent<PlayerHp>();
        coolDownImage = GameObject.Find("Fill").GetComponent<Image>();
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
        playerController = GetComponent<PlayerController>();
        inputActions.Player.Attack.started += StartRush;
    }

    private void StartRush(InputAction.CallbackContext ctx)
    {
        if (CanDash())
        {
            onStartRush.Invoke();
            isRushing = true;
            if (dashRoutine != null)
            {
                StopCoroutine(dashRoutine);
            }
            dashRoutine = StartCoroutine(Dash_Move());

            playerHp.UpdateUnBeatTime(noDamageTime, false);
        }
    }

    private IEnumerator Dash_Move()
    {
        isDashing = true;
        // dashReadyEffect.SetActive(false);
        rb.linearVelocity = transform.forward * rushSpeed;
        rb.useGravity = false;
        Debug.Log("No Damage Start");
        yield return new WaitForSeconds(duringTime);
        rb.linearVelocity = Vector3.zero;
        rb.useGravity = true;
        onEndRush.Invoke();
        isRushing = false;
        float timeElapsed = 0f;
        while (timeElapsed < coolTime)
        {
            coolDownImage.fillAmount = timeElapsed / coolTime;
            timeElapsed += Time.deltaTime;
            yield return null;
        }
        coolDownImage.fillAmount = 1;
        // dashReadyEffect.transform.position = transform.position;
        // dashReadyEffect.SetActive(true);
        isDashing = false;
    }

    // Rush 기능을 사용 가능한 Energy 관리 체계

    // Energy 소모 함수 & 대쉬 가능 여부 반환 
    public bool CanDash()
    {
        if (isDashing) return false;
        // if (energy < consumeEnergy || isDashing) return false;
        // energy -= consumeEnergy;
        // RefreshUI();

        return true;
    }

    // Energy 충전 이벤트 수신
    public void RegenEnergy()
    {
        energy += earnEnergy;
        if (energy > maxEnergy)
        {
            energy = maxEnergy;
        }
        RefreshUI();
    }

    private void RefreshUI()
    {
        dashEnergyText.text = $"Energy : {energy}";
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Enemy") && isRushing)
        {
            Enemy enemy = collision.gameObject.GetComponentInParent<Enemy>();
            if (enemy != null)
            {
                enemy.Kill();
            }
        }
    }
}
