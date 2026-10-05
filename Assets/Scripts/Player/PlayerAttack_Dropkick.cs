using System.Collections;
using UnityEngine;

/// <summary>
/// 돌진 공격 중 드롭킥 판정과 애니메이션 오브젝트를 관리한다.
/// </summary>
public class PlayerAttack_Dropkick : MonoBehaviour, IDamageSource
{
    private InputSystem_Actions inputActions;
    private RaycastHit hit;
    public BoxCollider dropkickRangeRb;
    public GameObject kickObject;
    public float kickSeconds = 0.25f;

    /// <summary>
    /// 드롭킥 범위 콜라이더에 닿은 적에게 Dropkick 피해를 전달한다.
    /// target은 피해를 받을 객체이며, ChainDamage 증강이 적용된 피해 정보를 target.TakeDamage로 보낸다.
    /// </summary>
    /// <param name="target">피해를 받을 객체</param>
    public void ApplyDropkickDamage(IDamageable target)
    {
        int damage = PlayerStats.Instance == null ? 5 : PlayerStats.Instance.ApplyInt(StatType.ChainDamage, 5);
        target.TakeDamage(new DamageInfo(damage, DamageKind.Dropkick, this));
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        inputActions = new InputSystem_Actions();
        dropkickRangeRb = GameObject.Find("Dropkick_Range").GetComponent<BoxCollider>();
    }

    void Start()
    {
        kickObject.SetActive(false);
    }

    void Update()
    {
        //inputActions.Player.Attack.performed += ctx => Dropkick();
    }

    void OnEnable()
    {
        inputActions.Enable();
    }

    void OnDisable()
    {
        inputActions.Disable();
    }

    /// <summary>
    /// 프리팹의 Rush 시작 UnityEvent가 호출하는 드롭킥 동작을 시작한다.
    /// DropkickSequence 코루틴을 실행해 공격 범위와 발 이펙트를 활성화한다.
    /// </summary>
    public void Dropkick()
    {
        StartCoroutine(DropkickSequence());
    }

    /// <summary>
    /// 드롭킥 공격 범위를 kickSeconds 동안 활성화하고 킥 오브젝트를 앞으로 이동시킨 뒤 원위치로 복귀한다.
    /// kickSeconds와 대기 시간을 사용하며 콜라이더와 킥 오브젝트 활성 상태를 변경한다.
    /// </summary>
    private IEnumerator DropkickSequence()
    {
        //박스 콜라이더와 킥 오브젝트 활성화
        dropkickRangeRb.enabled = true;
        kickObject.SetActive(true);

        //킥 오브젝트를 kickSeconds 동안 앞으로 부드럽게 이동시킴
        float timeElapsed = 0f;
        while (timeElapsed < kickSeconds)
        {
            timeElapsed += Time.deltaTime;
            kickObject.transform.localPosition = new Vector3(0f, 0f, Mathf.Lerp(0f, 1f, timeElapsed / kickSeconds));
            yield return null;
        }
        yield return new WaitForSeconds(0.25f);
        //킥 오브젝트 원위치 및 박스 콜라이더, 킥 오브젝트 비활성화
        kickObject.transform.localPosition = new Vector3(0f, 0.5f, 0f);
        kickObject.SetActive(false);
        dropkickRangeRb.enabled = false;
    }
}
