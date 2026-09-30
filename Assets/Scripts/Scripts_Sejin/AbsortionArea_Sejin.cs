using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

public class AbsortionArea_Sejin : MonoBehaviour
{
    public UnityEvent onGatherEnergy;
    public float slowMultiplier;
    [Tooltip("흡수 쿨 타임")]
    public float absorbTime;

    private List<Enemy> enemies = new List<Enemy>();
    private float nextAbsorbTime = 0f;

    private EnemyPool enemyPool;
    [Tooltip("한번에 흡수 가능한 적 수")]
    private float maxAbsorbCount = 5f;

    private void Awake()
    {
        enemyPool = GameObject.Find("ObjectPool").GetComponent<EnemyPool>();
    }
    private void Start()
    {
        // StartCoroutine(AbsorbEnemy());
        nextAbsorbTime = Time.time + absorbTime;
    }

    private void Update()
    {
        TryAbsorb();
        TryAbsorbSlow();
    }


    void OnTriggerEnter(Collider other)
    {

        if (!other.CompareTag("Enemy"))
            return;

        Enemy enemy = other.GetComponent<Enemy>();

        enemies.Add(enemy);
        //NavMeshAgent agent = other.GetComponent<NavMeshAgent>();
        //agent.speed = agent.speed * slowMultiplier;
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Enemy"))
            return;

        Enemy enemy = other.GetComponent<Enemy>();
        //NavMeshAgent agent = other.GetComponent<NavMeshAgent>();

        //agent.speed = agent.speed / slowMultiplier;
        enemies.Remove(enemy);
    }

    /// <summary>
    /// 흡수 공간 내 적들 슬로우
    /// </summary>
    private void TryAbsorbSlow()
    {
        foreach (Enemy enemy in enemies)
        {
            NavMeshAgent agent = enemy.GetComponent<NavMeshAgent>();
            if (agent.speed == enemy.speed)
            {
                agent.speed *= slowMultiplier;
            }
        }
    }

    /// <summary>
    /// 흡수 공간 내 적들 흡수 (최단거리)
    /// </summary>
    private void TryAbsorb()
    {
        // 아직 쿨다운 중
        if (Time.time < nextAbsorbTime)
            return;

        //이미 없어진 적들은 제외
        enemies.RemoveAll(e => e == null);

        //enemies에 있는 적들을 거리 기준으로 정렬 (0번째가 가장 가까움)
        enemies.Sort(CompareEnemiesDistance);

        //흡수 가능한 적 수랑 현재 배열에 들어있는 적 수 비교 (OutOfRange 방지)
        float absorableCount = maxAbsorbCount < enemies.Count ? maxAbsorbCount : enemies.Count;

        //가장 가까운 적 5마리 죽이기
        for (int curAbsorbIndex = 0; curAbsorbIndex < absorableCount; curAbsorbIndex++)
        {
            Enemy absorbTarget = enemies[curAbsorbIndex];
            onGatherEnergy?.Invoke();
            Debug.Log($"nearest Speed : {absorbTarget.speed}");
            StartCoroutine(absorbTarget.Die(false));
        }
        nextAbsorbTime = Time.time + absorbTime;
    }

    /// <summary>
    /// 두 유닛 중 더 가까운 누가 더 가까운지 확인
    /// </summary>
    /// <param name="a">비교대상 1</param>
    /// <param name="b">비교대상 2</param>
    /// <returns>더 가까우면 -1(앞으로), 더 멀면 1(뒤로)</returns>
    int CompareEnemiesDistance(Enemy a, Enemy b)
    {
        Vector3 myPos = transform.position;
        //각 적들과 본인 사이의 거리 계산
        float distA = (myPos - a.transform.position).sqrMagnitude;
        float distB = (myPos - b.transform.position).sqrMagnitude;
        return distA.CompareTo(distB);
    }

    private void OnDisable()
    {
        enemies.Clear();
    }
}
