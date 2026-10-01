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

    private Collider[] results = new Collider[50];
    private Vector3 boxSize;

    private void Awake()
    {
        areaCollider = GetComponent<BoxCollider>();
        boxSize = new Vector3(transform.lossyScale.x, 2f, transform.lossyScale.z);
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

        // enemies.Add(enemy);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Enemy"))
            return;

        Enemy enemy = other.GetComponent<Enemy>();

        // enemies.Remove(enemy);
    }

    public void AbsorbNearestOnce()
    {
        if (!isActiveAndEnabled || !canAbsorb)
            return;

        canAbsorb = false;

        Vector3 center = transform.position;
        Vector3 half = boxSize / 2f;
        Quaternion orientation = transform.rotation;

        int hitCount = Physics.OverlapBoxNonAlloc(center, half, results, orientation, ~(1 << 8));

        Collider nearest = null;
        float minDistance = float.MaxValue;

        for (int i = 0; i < hitCount; i++)
        {
            if (!results[i].gameObject.CompareTag("Enemy") || results[i].isTrigger) continue;
            float distance = Vector3.Distance(transform.root.position, results[i].transform.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearest = results[i];
            }
        }

        if (nearest == null)
        {
            return;
        }
        Enemy enemy = nearest.GetComponent<Enemy>();

        enemies.Remove(enemy);

        onGatherEnergy?.Invoke();
        enemy.DieAbsorbedEnemy();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, boxSize);
    }

    public void AbsorbNearestOnceLegacy()
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
