using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class AbsortionArea_Sejin : MonoBehaviour
{
    public UnityEvent onGatherEnergy;
    public float slowMultiplier;
    public float absorbTime;

    private List<Enemy> enemies = new List<Enemy>();
    private float nextAbsorbTime = 0f;

    private EnemyPool enemyPool;
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
    }


    void OnTriggerEnter(Collider other)
    {
        
        if (!other.CompareTag("Enemy"))
            return;

        Enemy enemy = other.GetComponentInParent<Enemy>();

        if (enemy == null)
        {
            onGatherEnergy.Invoke();
            // 슬로우
            // 소량 처치
            enemyPool.DieEnemy(other.gameObject);
        }

        enemies.Add(enemy);
        enemy.speed *= slowMultiplier;
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Enemy"))
            return;

        Enemy enemy = other.GetComponent<Enemy>();

        if (enemy == null)
            return;

        enemy.speed /= slowMultiplier;
        enemies.Remove(enemy);
    }

    private void TryAbsorb()
    {
        // 아직 쿨다운 중
        if (Time.time < nextAbsorbTime)
            return;

        Enemy nearest = null;
        float minDistance = float.MaxValue;

        foreach (Enemy enemy in enemies)
        {
            if (enemy == null)
                continue;

            float distance = Vector3.Distance(
                transform.position,
                enemy.transform.position
            );

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
        enemyPool.DieEnemy(nearest.gameObject);
        Debug.Log("absorb at " + Time.time);

        nextAbsorbTime = Time.time + absorbTime;
    }

    private void OnDisable()
    {
        foreach (Enemy enemy in enemies)
        {
            if (enemy != null)
                enemy.speed /= slowMultiplier;
        }

        enemies.Clear();
    }
}
