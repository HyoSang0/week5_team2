using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;
using System.Collections;

public class RushAbility_Sejin : MonoBehaviour
{
    public float energy = 0.0f;
    public float earnEnergy = 5.0f;
    public float consumeEnergy = 10.0f;
    public float maxEnergy = 30.0f;
    private InputSystem_Actions inputActions;
    private PlayerController playerController;

    public UnityEvent onStartRush;
    public UnityEvent onEndRush;

    public bool isRushing = false;

    void Awake()
    {
        energy = 0.0f;
        inputActions = new InputSystem_Actions();
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
        inputActions.Player.Attack.canceled += EndRush;
    }

    private void StartRush(InputAction.CallbackContext ctx)
    {
        onStartRush.Invoke();
        isRushing = true;
        StartCoroutine(ConsumeEnergy());
    }

    private void EndRush(InputAction.CallbackContext ctx)
    {
        StopCoroutine(ConsumeEnergy());
        isRushing = false;
        onEndRush.Invoke();
    }

    // Rush 기능을 사용 가능한 Energy 관리 체계

    // Energy 소모 코루틴
    public IEnumerator ConsumeEnergy()
    {
        while(isRushing)
        {
            energy -= consumeEnergy * Time.deltaTime;
            if(energy < 0.0f)
            {
                onEndRush.Invoke();
                energy = 0.0f;
            }
            yield return null;
        }
    }

    // Energy 충전 이벤트 수신
    public void RegenEnergy()
    {
        energy += earnEnergy;
        if (energy > maxEnergy)
        {
            energy = maxEnergy;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if(collision.gameObject.CompareTag("Enemy") && isRushing)
        {
            Destroy(collision.gameObject);
        }
    }
}
