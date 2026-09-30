using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class AbsortionArea_Sejin : MonoBehaviour
{
    public UnityEvent onGatherEnergy;
    public float slowMultiplier;
    public float absorbTime;
    private HashSet<Enemy> enemySet = new HashSet<Enemy>();
    private HashSet<Enemy> absorbSet = new HashSet<Enemy>();

    private float holdingTime = 0f;
    [SerializeField]
    private float targetAddTime = 0.1f;
    private void Awake()
    {

    }

    private void OnEnable()
    {
        absorbSet.Clear();
        enemySet.Clear();
        holdingTime = 0f;
    }

    private void OnDisable()
    {
        AbsorbAll();
        enemySet.Clear();
    }

    private void Update()
    {
        // 홀딩 타임 증가
        holdingTime += Time.deltaTime;
        // 홀딩 타임이 애딩 타임 오바
        if (holdingTime >= targetAddTime)
        {
            // 타겟 추가
            var nextTarget = NextAbsorbTarget();
            if (nextTarget != null)
            {
                enemySet.Remove(nextTarget);
                absorbSet.Add(nextTarget);
                // 흡수 대상 지정 → 노란 오러 셸 표시
                nextTarget.OnAbsorbTarget(true);
            }
            // 홀딩 타임 초기화
            holdingTime = 0f;
        }
    }


    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Enemy"))
            return;
        Enemy enemy = other.GetComponent<Enemy>();
        Debug.Log($"OnTrigger Enter {enemy}");
        if (!absorbSet.Contains(enemy))
            enemySet.Add(enemy);
    }
    private void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag("Enemy"))
            return;

        Enemy enemy = other.GetComponent<Enemy>();
        Debug.Log($"OnTrigger Stay {enemy}");
        if (!absorbSet.Contains(enemy))
            enemySet.Add(enemy);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Enemy"))
            return;

        Enemy enemy = other.GetComponent<Enemy>();
        Debug.Log($"OnTrigger Exit {enemy}");
        if (absorbSet.Contains(enemy))
        {
            absorbSet.Remove(enemy);
            // 영역 이탈 시 노란 오러 셸 해제
            enemy.OnAbsorbTarget(false);
            var next = NextAbsorbTarget();
            absorbSet.Add(next);
            enemySet.Remove(next);
        }
        else
            enemySet.Remove(enemy);
    }

    private Enemy NextAbsorbTarget()
    {
        Enemy nearest = null;
        float minDistance = float.MaxValue;

        foreach (Enemy enemy in enemySet)
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

        return nearest;
    }

    /// <summary>
    /// 흡수 처리
    /// </summary>
    /// <param name="enemy"></param>
    private void Absorb(Enemy enemy)
    {
        // 이미 풀로 반환(비활성)된 적은 건너뜀
        if (!enemy.gameObject.activeInHierarchy)
            return;

        onGatherEnergy?.Invoke();
        // 이 영역은 OnDisable 중(비활성)에 호출되므로 코루틴은 적 쪽에서 실행
        enemy.DoDie(false);
    }

    private void AbsorbAll()
    {
        foreach (Enemy target in absorbSet)
            Absorb(target);
        absorbSet.Clear();
    }
}
