using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class RushAbility_Sejin : MonoBehaviour
{
    public TextMeshProUGUI dashEnergyText;
    public float energy = 0.0f;
    public float earnEnergy = 5.0f;
    public float consumeEnergy = 10.0f;
    public float maxEnergy = 30.0f;
    public float rushSpeed = 50.0f;
    public float duringTime = 0.2f;
    public float noDamageTime = 5.0f;
    public float coolTime = 1.0f;
    public bool isDashing = false;

    private Coroutine dashRoutine;
    private Coroutine unbeatRoutine;

    private Rigidbody rb;
    private InputSystem_Actions inputActions;
    private PlayerController playerController;
    private PlayerHp playerHp;

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
            if (dashRoutine != null)
            {
                StopCoroutine(dashRoutine);
            }
            dashRoutine = StartCoroutine(Dash_Move());

            playerHp.UpdateUnBeatTime(noDamageTime);
        }
    }

    private IEnumerator Dash_Move()
    {
        isDashing = true;
        rb.linearVelocity = transform.forward * rushSpeed;
        rb.useGravity = false;
        Debug.Log("No Damage Start");
        yield return new WaitForSeconds(duringTime);
        rb.linearVelocity = Vector3.zero;
        rb.useGravity = true;
        onEndRush.Invoke();
        yield return new WaitForSeconds(coolTime);
        isDashing = false;
    }

    // Rush 기능을 사용 가능한 Energy 관리 체계

    // Energy 소모 함수 & 대쉬 가능 여부 반환 
    public bool CanDash()
    {
        if (energy < consumeEnergy || isDashing) return false;

        energy -= consumeEnergy;
        RefreshUI();

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
            Enemy enemy = collision.gameObject.GetComponent<Enemy>();
            enemy.Die(true);
        }
    }
}
