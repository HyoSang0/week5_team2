using System.Collections.Generic;

using UnityEngine;

public class AbsortionArea_Sejin : MonoBehaviour
{
    [SerializeField] private EnemyAbsorbEffect lightBallPrefab;
    // 빛 구슬의 목적지가 UiMarker 모드일 때 날아갈 위치
    [SerializeField] private Transform _uiWorldMarker;

    [Header("Conversion Absorb")]
    // true면 영역에 들어온 적을 enemySet에 모으지 않고 즉시 흡수한다
    [SerializeField] private bool _absorbOnEnter;
    // 영역 외곽선(LineRenderer) 색. 기본값은 흰색이다
    [SerializeField] private Color _outlineColor = Color.white;

    private Transform playerTarget;
    public float slowMultiplier;
    public float absorbTime;
    private HashSet<Enemy> enemySet = new HashSet<Enemy>();

    [SerializeField] private float area_radus_max, area_radus_min;
    protected float area_radus;
    // 증강 스탯 재계산에 사용할 기준값들
    private float _baseAreaRadiusMin;
    private float _baseAreaRadiusMax;
    private float _baseSpeed;
    protected Vector3 originScale;
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
        _baseSpeed = speed;

        // 전환 영역처럼 색을 지정한 경우에만 외곽선 색을 바꾼다. 기본 영역은 흰색이라 변하지 않는다.
        if (outline != null)
        {
            outline.startColor = _outlineColor;
            outline.endColor = _outlineColor;
        }
    }

    /// <summary>
    /// PlayerStats의 AbsorbRadius, AbsorbGrowSpeed 증강을 기준값에 적용해 area_radus_min, area_radus_max, speed를 재계산한다.
    /// 반지름 두 값은 같은 스탯의 배율로 적용한다. PlayerStats.Instance가 없으면 아무것도 하지 않는다.
    /// </summary>
    private void ApplyAugmentStats()
    {
        if (PlayerStats.Instance == null)
        {
            return;
        }

        area_radus_min = PlayerStats.Instance.Apply(StatType.AbsorbRadius, _baseAreaRadiusMin);
        area_radus_max = PlayerStats.Instance.Apply(StatType.AbsorbRadius, _baseAreaRadiusMax);
        speed = PlayerStats.Instance.Apply(StatType.AbsorbGrowSpeed, _baseSpeed);
    }


    private void Start()
    {
        // Start는 씬의 모든 Awake 이후 실행되므로 여기서 구독하면 PlayerStats.Awake 순서와 무관하다.
        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.OnStatsChanged += HandleStatsChanged;
        }

        ApplyAugmentStats();
    }

    private void OnDestroy()
    {
        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.OnStatsChanged -= HandleStatsChanged;
        }
    }

    /// <summary>
    /// 스탯 변경 시 호출되어 AbsorbRadius, AbsorbGrowSpeed 증강을 다시 적용하고, 흡수 영역이 활성이면 현재 반경을 새 최소/최대 범위로 클램프해 스케일을 동기화한다.
    /// 비활성 상태에서는 값만 갱신하고 transform은 건드리지 않으며, 실제 반영은 활성화 시 OnEnable에서 수행한다.
    /// </summary>
    private void HandleStatsChanged()
    {
        ApplyAugmentStats();

        if (!isActiveAndEnabled)
        {
            return;
        }

        area_radus = Mathf.Clamp(area_radus, area_radus_min, area_radus_max);
        ApplyAreaScale();
    }

    /// <summary>
    /// 현재 area_radus를 영역 오브젝트의 스케일에 반영한다.
    /// area_radus와 originScale을 사용하며, 기본 구현은 transform.localScale을 originScale * area_radus로 설정한다.
    /// 영역 모양이 다른 서브클래스가 스케일 적용 방식을 바꾸기 위한 가상 훅이다.
    /// </summary>
    protected virtual void ApplyAreaScale()
    {
        transform.localScale = originScale * area_radus;
    }

    private void OnEnable()
    {
        // 활성화 시점에 현재 스탯을 읽어 영역의 초기 반경과 확장 속도에 반영한다.
        ApplyAugmentStats();

        enemySet.Clear();
        area_radus = area_radus_min;
        ApplyAreaScale();
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

        ApplyAreaScale();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Enemy"))
            return;
        Enemy enemy = other.GetComponent<Enemy>();
        if (!CanCapture(enemy))
            return;

        // 흡수 즉시 실행 모드에서는 대상을 모아 두지 않고 들어오는 즉시 흡수한다.
        if (_absorbOnEnter)
        {
            Absorb(enemy);
            return;
        }

        enemy.OnAbsorbTarget(true);
        enemySet.Add(enemy);
    }
    private void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag("Enemy"))
            return;

        Enemy enemy = other.GetComponent<Enemy>();
        if (!CanCapture(enemy))
            return;

        // 흡수 즉시 실행 모드에서는 Stay에서도 늦게 들어온 적을 즉시 흡수한다.
        if (_absorbOnEnter)
        {
            Absorb(enemy);
            return;
        }

        if (enemySet.Add(enemy))
            enemy.OnAbsorbTarget(true);
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
    /// 적이 이 흡수 영역의 대상이 될 수 있는지 판정한다.
    /// enemy는 Enemy 태그 충돌에서 가져온 컴포넌트이며 기본 구현은 null이 아니면 true를 반환한다.
    /// </summary>
    protected virtual bool CanCapture(Enemy enemy)
    {
        return enemy != null;
    }

    /// <summary>
    /// 접촉한 적의 즉시 흡수를 시도한다. 흡수 반경 판정 없이 적격성만 확인한다.
    /// enemy는 접촉 판정에서 얻은 Enemy이며, null이거나 흡수 대상이 아니면 false를 반환한다.
    /// </summary>
    public bool TryAbsorbOnContact(Enemy enemy)
    {
        if (enemy == null || !CanCapture(enemy))
        {
            return false;
        }

        return enemy.TryAbsorb(lightBallPrefab, playerTarget, _uiWorldMarker);
    }

    /// <summary>
    /// 적이 현재 흡수 반경 안에 들어왔는지 판정한다.
    /// enemy의 위치와 area_radus, originScale을 사용하며 기본 구현은 보정값 0.1f를 더한 거리가 현재 월드 반경 이내면 true를 반환한다.
    /// </summary>
    protected virtual bool IsInAbsorbRange(Enemy enemy)
    {
        // 간헐적으로 새로 스폰된 적이 흡수되는 문제를 막기 위해 보정값을 더한 거리로 판정한다.
        float currentWorldRadius = area_radus * originScale.x;
        float distance = Vector3.Distance(transform.position, enemy.transform.position) + 0.1f;
        return distance <= currentWorldRadius;
    }

    /// <summary>
    /// 흡수 후보의 적격성과 현재 영역의 범위를 확인한 뒤 대상의 흡수를 시도한다.
    /// enemy가 판정을 통과하면 TryAbsorb를 호출하고, 실패하면 흡수 대상 표시를 해제한다.
    /// </summary>
    private void Absorb(Enemy enemy)
    {
        if (enemy == null || !enemy.gameObject.activeInHierarchy)
            return;

        // 범위 판정은 서브클래스가 바꿀 수 있도록 가상 술어로 위임한다.
        if (!CanCapture(enemy) || !IsInAbsorbRange(enemy))
        {
            enemy.OnAbsorbTarget(false);
            return;
        }

        if (!enemy.TryAbsorb(lightBallPrefab, playerTarget, _uiWorldMarker))
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
