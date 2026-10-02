using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class AbsortionArea_Sejin : MonoBehaviour
{
    [SerializeField] private EnemyAbsorbEffect lightBallPrefab;
    // 빛 구슬의 목적지가 UiMarker 모드일 때 날아갈 위치
    [SerializeField] private Transform _uiWorldMarker;
    private Transform playerTarget;
    public UnityEvent onGatherEnergy;
    public float slowMultiplier;
    public float absorbTime;
    private HashSet<Enemy> enemySet = new HashSet<Enemy>();

    [SerializeField] private float area_radus_max = 8f, area_radus_min = 4f;
    private float area_radus;
    // 증강 스탯 재계산에 사용할 반지름 기준값들
    private float _baseAreaRadiusMin;
    private float _baseAreaRadiusMax;
    private Vector3 originScale;
    [SerializeField]
    private float speed;


    // effect 영역

    [SerializeField]
    LineRenderer outline;
    public int segments = 64;
    public float outlineWidth = 0.08f;

    private void Awake()
    {
        originScale = transform.localScale;
        area_radus = area_radus_min;
        PlayerController player = GetComponentInParent<PlayerController>();
        playerTarget = player != null ? player.transform : null;

        _baseAreaRadiusMin = area_radus_min;
        _baseAreaRadiusMax = area_radus_max;
    }

    /// <summary>
    /// PlayerStats의 AbsorbRadius 증강을 기준값에 적용해 area_radus_min, area_radus_max를 재계산한다.
    /// 두 값 모두 같은 스탯의 배율로 적용한다. PlayerStats.Instance가 없으면 아무것도 하지 않는다.
    /// </summary>
    private void ApplyAugmentStats()
    {
        if (PlayerStats.Instance == null)
        {
            return;
        }

        area_radus_min = PlayerStats.Instance.Apply(StatType.AbsorbRadius, _baseAreaRadiusMin);
        area_radus_max = PlayerStats.Instance.Apply(StatType.AbsorbRadius, _baseAreaRadiusMax);
    }


    private void OnEnable()
    {
        // Start 실행 시점이 첫 활성화로 밀려 구독 대신 활성화 시점에 현재 스탯을 읽는다.
        ApplyAugmentStats();

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

        if (!enemy.TryAbsorb(lightBallPrefab, playerTarget, onGatherEnergy, _uiWorldMarker))
            enemy.OnAbsorbTarget(false);
    }

    private void AbsorbAll()
    {
        Enemy[] targets = new Enemy[enemySet.Count];
        enemySet.CopyTo(targets);

        foreach (Enemy target in targets)
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
