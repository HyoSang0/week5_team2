using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(BoxCollider))]
public class AbsortionArea_Sejin : MonoBehaviour
{
    public UnityEvent onGatherEnergy;

    private List<Enemy> enemies = new List<Enemy>();
    private BoxCollider areaCollider;
    private bool canAbsorb = true;

    private void Awake()
    {
        areaCollider = GetComponent<BoxCollider>();
    }

    private void OnEnable()
    {
        canAbsorb = true;
    }
    private void OnDisable()
    {
        canAbsorb = true;
    }

    void OnTriggerEnter(Collider other)
    {

        if (!other.CompareTag("Enemy"))
            return;

        Enemy enemy = other.GetComponent<Enemy>();

        enemies.Add(enemy);
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

    public void AbsorbNearestOnce()
    {
        if (!isActiveAndEnabled || !canAbsorb)
            return;

        canAbsorb = false;

        Enemy nearest = null;
        float minDistance = float.MaxValue;

        foreach (Enemy enemy in enemies)
        {
            if (enemy == null)
            {
                continue;
            }

            float distance = Vector3.Distance(transform.root.position, enemy.transform.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearest = enemy;
            }
        }

        if (nearest == null)
        {
            return;
        }

        enemies.Remove(nearest);

        onGatherEnergy?.Invoke();
        nearest.isDead = true;
        StartCoroutine(nearest.Die(false));
    }
}
