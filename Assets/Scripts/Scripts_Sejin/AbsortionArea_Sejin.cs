using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
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
        Debug.Log($"nearest Speed : {nearest.speed}");
        StartCoroutine(nearest.Die(false));
        

        nextAbsorbTime = Time.time + absorbTime;
    }

    private void OnDisable()
    {
        

        enemies.Clear();
    }
}
