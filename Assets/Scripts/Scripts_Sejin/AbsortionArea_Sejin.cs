using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class AbsortionArea_Sejin : MonoBehaviour
{
    public UnityEvent onGatherEnergy;
    public float slowMultiplier;
    public float absorbTime;
    private HashSet<Enemy> enemySet = new HashSet<Enemy>();

    [SerializeField]
    private float area_radus_max = 8f, area_radus_min = 4f;
    private float area_radus;
    private Vector3 originScale;
    [SerializeField]
    private float speed;


    // effect 영역

    [SerializeField]
    LineRenderer outline;

    public float warningTime = 3f;
    public int segments = 64;
    public float outlineWidth = 0.08f;

    private void Awake()
    {
        originScale = transform.localScale;
        area_radus = area_radus_min;
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
        CreateOutline(area_radus);

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
        // 이미 풀로 반환(비활성)된 적은 건너뜀
        if (!enemy.gameObject.activeInHierarchy)
            return;

        onGatherEnergy?.Invoke();
        // 이 영역은 OnDisable 중(비활성)에 호출되므로 코루틴은 적 쪽에서 실행
        enemy.DoDie(false);
    }

    private void AbsorbAll()
    {
        foreach (Enemy target in enemySet)
            Absorb(target);
    }


    private void CreateOutline(float radius)
    {
        outline.positionCount = segments;
        outline.loop = true;
        outline.useWorldSpace = false;

        outline.startWidth = outlineWidth;
        outline.endWidth = outlineWidth;

        for (int i = 0; i < segments; i++)
        {
            float angle = (float)i / segments * Mathf.PI * 2f;

            float x = Mathf.Cos(angle) * radius / 8;
            float z = Mathf.Sin(angle) * radius / 8;

            outline.SetPosition(i, new Vector3(x, 0.01f, z));
        }
    }
}
