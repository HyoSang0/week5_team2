using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class AbsortionArea_Sejin : MonoBehaviour
{
    public UnityEvent onGatherEnergy;

    [SerializeField, Min(0.1f)] private float radius = 1f;
    [SerializeField] private float indicatorHeight = 0.12f;
    [SerializeField, Min(3)] private int indicatorSegments = 64;

    private const int MaxColliderBufferSize = 2048;
    private readonly HashSet<Enemy> scannedEnemies = new HashSet<Enemy>();
    private Collider[] colliderBuffer = new Collider[64];
    private CapsuleCollider playerCollider;
    private AbsortionAbility_Sejin ability;
    private LineRenderer indicator;
    private bool warnedAboutFullBuffer;

    private void Awake()
    {
        radius = Mathf.Max(0.1f, radius);
        indicatorSegments = Mathf.Max(3, indicatorSegments);
        playerCollider = GetComponentInParent<CapsuleCollider>();
        if (playerCollider != null)
            ability = playerCollider.GetComponent<AbsortionAbility_Sejin>();

        // 이전 전방 BoxCollider와 사각형 메시는 씬에 남기되 사용하지 않는다.
        BoxCollider oldRange = GetComponent<BoxCollider>();
        if (oldRange != null)
            oldRange.enabled = false;

        MeshRenderer oldVisual = GetComponent<MeshRenderer>();
        if (oldVisual != null)
            oldVisual.enabled = false;

        indicator = GetComponent<LineRenderer>();
        if (indicator == null)
            indicator = gameObject.AddComponent<LineRenderer>();

        indicator.sharedMaterial = Resources.Load<Material>("Materials/MT_AbilityArea");
        indicator.useWorldSpace = true;
        indicator.loop = true;
        indicator.positionCount = indicatorSegments;
        indicator.startWidth = 0.04f;
        indicator.endWidth = 0.04f;
        indicator.numCapVertices = 2;
    }

    private void OnEnable()
    {
        indicator.enabled = ability != null
            && ability.State == AbsortionAbility_Sejin.AbsorptionState.Active;
        if (!indicator.enabled)
            return;

        DrawIndicator();
        AbsorbNearbyEnemies();
    }

    private void Update()
    {
        DrawIndicator();
    }

    private void FixedUpdate()
    {
        if (ability != null && ability.State == AbsortionAbility_Sejin.AbsorptionState.Active)
            AbsorbNearbyEnemies();
    }

    private void OnDisable()
    {
        scannedEnemies.Clear();
    }

    private void AbsorbNearbyEnemies()
    {
        if (playerCollider == null)
            return;

        Vector3 center = playerCollider.transform.TransformPoint(playerCollider.center);
        int count;

        // 한 번에 여러 적을 잡더라도 고정 배열이 가득 차서 대상을 누락하지 않도록 확장한다.
        while (true)
        {
            count = Physics.OverlapSphereNonAlloc(
                center, radius, colliderBuffer, ~0, QueryTriggerInteraction.Ignore);

            if (count < colliderBuffer.Length || colliderBuffer.Length >= MaxColliderBufferSize)
                break;

            Array.Resize(ref colliderBuffer,
                Mathf.Min(colliderBuffer.Length * 2, MaxColliderBufferSize));
        }

        if (count == colliderBuffer.Length && !warnedAboutFullBuffer)
        {
            Debug.LogWarning("Absorption collider buffer is full. Some enemies may be missed.", this);
            warnedAboutFullBuffer = true;
        }

        scannedEnemies.Clear();
        for (int i = 0; i < count; i++)
        {
            Collider hit = colliderBuffer[i];
            if (hit == null)
                continue;

            Enemy enemy = hit.GetComponentInParent<Enemy>();
            if (enemy == null || !enemy.CompareTag("Enemy") || !scannedEnemies.Add(enemy))
                continue;

            if (enemy.TryAbsorb())
                onGatherEnergy?.Invoke();
        }
    }

    private void DrawIndicator()
    {
        if (playerCollider == null)
            return;

        Vector3 center = playerCollider.transform.position + Vector3.up * indicatorHeight;
        for (int i = 0; i < indicatorSegments; i++)
        {
            float angle = i * Mathf.PI * 2f / indicatorSegments;
            indicator.SetPosition(i, center + new Vector3(
                Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius));
        }
    }

#if false // 이전 전방 트리거 방식. Overlap 일괄 처치로 대체했다.
    public float slowMultiplier;
    public float absorbTime;
    private readonly List<Enemy> enemies = new List<Enemy>();
    private float nextAbsorbTime;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy"))
            enemies.Add(other.GetComponent<Enemy>());
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Enemy"))
            enemies.Remove(other.GetComponent<Enemy>());
    }

    private void TryAbsorbSlow()
    {
        foreach (Enemy enemy in enemies)
        {
            var agent = enemy.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent.speed == enemy.speed)
                agent.speed *= slowMultiplier;
        }
    }

    private void TryAbsorb()
    {
        if (Time.time < nextAbsorbTime)
            return;

        Enemy nearest = null;
        float minDistance = float.MaxValue;
        foreach (Enemy enemy in enemies)
        {
            if (enemy == null)
                continue;
            float distance = Vector3.Distance(transform.position, enemy.transform.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearest = enemy;
            }
        }
        if (nearest == null)
            return;

        enemies.Remove(nearest);
        onGatherEnergy?.Invoke();
        StartCoroutine(nearest.Die(false));
        nextAbsorbTime = Time.time + absorbTime;
    }
#endif
}
