using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;
using System.Collections;
using TMPro;

public class RushAbility_Sejin : MonoBehaviour
{
    public TextMeshProUGUI dashEnergyText;
    public float energy = 0.0f;
    public float earnEnergy = 5.0f;
    public float consumeEnergy = 10.0f;
    public float maxEnergy = 30.0f;
    public float rushSpeed = 50.0f;
    public float duringTime = 0.2f;
    public float noDamageTime = 0.3f;
    public float coolTime = 1.0f;
    public bool isDashing = false;
    public bool canDash = false;

    private Rigidbody rb;
    private InputSystem_Actions inputActions;
    private PlayerController playerController;

    public UnityEvent onStartRush;
    public UnityEvent onEndRush;

    public bool isRushing = false;

    private EnemyPool enemyPool;

    void Awake()
    {
        energy = 0.0f;
        inputActions = new InputSystem_Actions();
        rb = GetComponent<Rigidbody>();
        enemyPool = GameObject.Find("ObjectPool").GetComponent<EnemyPool>();
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
        if(CanDash())
        {
            onStartRush.Invoke();
            StartCoroutine(Dash());
        }
    }

    private IEnumerator Dash()
    {
        canDash = false;
        isDashing = true;
        rb.linearVelocity = transform.forward * rushSpeed;
        rb.useGravity = false;
        yield return new WaitForSeconds(duringTime);
        rb.linearVelocity = Vector3.zero;
        isDashing = false;
        rb.useGravity = true;
        yield return new WaitForSeconds(noDamageTime);
        onEndRush.Invoke();
        yield return new WaitForSeconds(coolTime);
        canDash = true;
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
        if(collision.gameObject.CompareTag("Enemy") && isRushing)
        {
            Enemy enemy = collision.gameObject.GetComponent<Enemy>();
            enemy.Die(true);
            
        }
    }
}
