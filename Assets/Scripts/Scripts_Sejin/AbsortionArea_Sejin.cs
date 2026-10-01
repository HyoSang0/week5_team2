using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class AbsortionArea_Sejin : MonoBehaviour
{
    [SerializeField] private EnemyAbsorbEffect lightBallPrefab;
    private Transform playerTarget;
    public UnityEvent onGatherEnergy;
    public float slowMultiplier;
    public float absorbTime;
    private HashSet<Enemy> enemySet = new HashSet<Enemy>();

    [SerializeField] private float area_radus_max = 8f, area_radus_min = 4f;
    private float area_radus;
    private Vector3 originScale;
    [SerializeField] private float speed;
    private void Awake()
    {
        originScale = transform.localScale;
        area_radus = area_radus_min;
        PlayerController player = GetComponentInParent<PlayerController>();
        playerTarget = player != null ? player.transform : null;
    }

    private void OnEnable()
    {
        enemySet.Clear();
        transform.localScale = originScale * area_radus_min;
        area_radus = area_radus_min;
    }

    private void OnDisable()
    {
        AbsorbAll();
        enemySet.Clear();
        area_radus = area_radus_min;
    }

    private void Update()
    {
        area_radus += Time.deltaTime * speed;
        area_radus = Mathf.Clamp(area_radus, area_radus_min, area_radus_max);

        transform.localScale = originScale * area_radus;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Enemy"))
            return;
        Enemy enemy = other.GetComponent<Enemy>();
        enemy.OnAbsorbTarget(true);
        enemySet.Add(enemy);
    }
    private void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag("Enemy"))
            return;

        Enemy enemy = other.GetComponent<Enemy>();
        enemy.OnAbsorbTarget(true);
        enemySet.Add(enemy);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Enemy"))
            return;

        Enemy enemy = other.GetComponent<Enemy>();
        enemy.OnAbsorbTarget(false);
        enemySet.Remove(enemy);
    }

    /// <summary>
    /// 흡수 처리
    /// </summary>
    /// <param name="enemy"></param>
    private void Absorb(Enemy enemy)
    {
        if (enemy == null)
            return;

        if (!enemy.TryAbsorb(lightBallPrefab, playerTarget, onGatherEnergy))
            enemy.OnAbsorbTarget(false);
    }

    private void AbsorbAll()
    {
        Enemy[] targets = new Enemy[enemySet.Count];
        enemySet.CopyTo(targets);

        foreach (Enemy target in targets)
            Absorb(target);
    }
}
