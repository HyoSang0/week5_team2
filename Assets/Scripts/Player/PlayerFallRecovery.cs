using UnityEngine;
using System.Collections;

public class PlayerFallRecovery : MonoBehaviour
{
    private const float RETRY_INTERVAL = 0.2f;

    [Header("Recovery Settings")]
    [SerializeField] private GroundInitializer ground;
    [SerializeField, Min(1)] private int fallDamage = 1;
    [SerializeField, Min(0.1f)] private float invincibilitySeconds = 2f;
    [SerializeField, Min(0f)] private float supportMargin = 0.1f;
    [SerializeField, Min(0.01f)] private float heightOffset = 0.05f;

    [Header("Runtime")]
    private PlayerHp playerHp;
    private RushAbility_Sejin rushAbility;
    private Rigidbody rb;
    private CapsuleCollider bodyCollider;
    private bool isRecovering;

    public bool IsRecovering => isRecovering;

    void Awake()
    {
        ground = FindFirstObjectByType<GroundInitializer>().GetComponent<GroundInitializer>();
        playerHp = GetComponent<PlayerHp>();
        rushAbility = GetComponent<RushAbility_Sejin>();
        rb = GetComponent<Rigidbody>();
        bodyCollider = GetComponent<CapsuleCollider>();
    }

    /// <summary>
    /// DeathZone 접촉 시 낙하 피해를 한 번 적용하고 생존하면 복귀를 시작한다. 
    /// 설정된 피해와 무적 시간을 사용하며, 처리 중이거나 사망한 경우 중복 실행하지 않는다. 
    /// </summary>
    public void HandleFall()
    {
        if (isRecovering || playerHp.playerHP <= 0)
        {
            return;
        }

        isRecovering = true;

        if (!playerHp.ApplyFallDamage(fallDamage, invincibilitySeconds))
        {
            return;
        }

        rushAbility.InterruptRushForFall();
        StartCoroutine(RecoverRoutine());
    }

    /// <summary>
    /// 낙하 시점의 위치와 몸체 크기로 가까운 복귀 지면을 찾는다. 
    /// 후보가 없으면 위치를 고정한 채 재시도하고, 찾으면 이동과 피격 무적을 적용한다. 
    /// </summary>
    private IEnumerator RecoverRoutine()
    {
        Vector3 fallPosition = transform.position;
        Bounds bodyBounds = bodyCollider.bounds;

        float supportRadius = Mathf.Max(bodyBounds.extents.x, bodyBounds.extents.z) + supportMargin;

        Vector3 footPosition = new Vector3(bodyBounds.center.x, bodyBounds.min.y, bodyBounds.center.z);
        Vector3 pivotOffset = transform.position - footPosition;
        bool wasKinematic = rb.isKinematic;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;

        Vector3 surfacePosition;

        WaitForSeconds sec = new WaitForSeconds(RETRY_INTERVAL);
        while (!ground.TryFindRecoverySurface(fallPosition, supportRadius, out surfacePosition))
        {
            yield return sec;

            if (playerHp.playerHP <= 0)
            {
                yield break;
            }
        }
        rb.position = surfacePosition + pivotOffset + Vector3.up * heightOffset;

        rb.isKinematic = wasKinematic;
        rb.useGravity = true;

        playerHp.ApplyHitInvincibility(invincibilitySeconds);

        yield return new WaitForFixedUpdate();
        yield return null;

        isRecovering = false;
    }
    // Update is called once per frame
    void Update()
    {

    }
}
