using System.Collections;

using UnityEngine;
using UnityEngine.AI;

public class SpawnManager : MonoBehaviour
{
    [Header("Spawn Schedule")]
    [SerializeField] private SpawnSchedule _schedule;
    [SerializeField] private float worldDia;
    [SerializeField] private GameObject player;
    [SerializeField] private GameObject healPack;

    [Header("Prewarm")]
    [SerializeField] private float _estimateDuration = 60f;
    [SerializeField] private int _minPrewarm = 5;
    [SerializeField] private int _maxPrewarm = 300;
    [SerializeField] private int _prewarmPerFrame = 30;

    [Header("Spawn Position")]
    [SerializeField] private float _minPlayerDistance = 6f;
    [SerializeField] private float _navSampleRadius = 1f;
    [SerializeField] private int _maxSpawnAttempts = 8;

    /// <summary>
    /// 웨이브 1개의 런타임 스폰 진행 상태. 다음 스폰 시각과 남은 스폰 횟수를 보관한다.
    /// </summary>
    private struct WaveState
    {
        public float NextTime;
        public int Remaining;
    }

    private EnemyPool enemyPool;
    private GameManager gameManager;
    private bool _hasWarnedSpawnFail;
    private bool _hasWarnedInvalidInterval;
    private WaveState[] _states;
    private float _elapsed;

    void Awake()
    {
        enemyPool = GetComponent<EnemyPool>();
        gameManager = GetComponent<GameManager>();
    }

    void Start()
    {
        if (_schedule == null)
        {
            Debug.LogWarning("SpawnSchedule이 할당되지 않아 스폰을 진행하지 않습니다.");
            _states = new WaveState[0];
            return;
        }

        // 웨이브 시작 전 프리웜을 프레임 분산으로 함께 진행한다.
        StartCoroutine(PrewarmRoutine());

        _states = new WaveState[_schedule.Waves.Count];
        for (int i = 0; i < _schedule.Waves.Count; i++)
        {
            SpawnWave wave = _schedule.Waves[i];
            WaveState state;
            state.NextTime = wave.StartTime;
            state.Remaining = CountRemaining(wave);
            _states[i] = state;
        }
    }

    void Update()
    {
        _elapsed += Time.deltaTime;

        // 도래하지 않은 스폰은 다음 프레임에 몰아서 처리하므로 프레임 지연과 무관하게 타이밍을 지킨다.
        for (int i = 0; i < _states.Length; i++)
        {
            SpawnWave wave = _schedule.Waves[i];
            WaveState state = _states[i];

            while (state.Remaining > 0 && _elapsed >= state.NextTime)
            {
                for (int c = 0; c < wave.Count; c++) SpawnOne(wave.Kind);
                state.NextTime += wave.Interval;
                state.Remaining--;
            }

            _states[i] = state;
        }
    }

    /// <summary>
    /// 웨이브가 시작부터 종료까지 수행할 스폰 횟수를 계산한다.
    /// wave의 Duration/Interval을 사용하며, Duration이 0 이하이면 1회, 그 외에는 Duration을 Interval만큼씩
    /// 소진하는 횟수를 반환한다. Interval이 0 이하이면서 Duration이 0보다 크면 1회로 처리하고 판당 1회 경고한다.
    /// </summary>
    private int CountRemaining(SpawnWave wave)
    {
        if (wave.Duration <= 0f)
        {
            return 1;
        }

        if (wave.Interval <= 0f)
        {
            if (!_hasWarnedInvalidInterval)
            {
                _hasWarnedInvalidInterval = true;
                Debug.LogWarning($"Duration이 {wave.Duration}초인데 Interval이 {wave.Interval} 이하인 웨이브는 1회만 스폰합니다.");
            }
            return 1;
        }

        float time = wave.Duration;
        int remaining = 0;
        while (time > 0f)
        {
            remaining++;
            time -= wave.Interval;
        }

        return remaining;
    }

    /// <summary>
    /// 지정된 종류의 유닛 1체를 검증된 위치에서 소환한다.
    /// kind를 사용하며, 힐팩이면 Instantiate, 적이면 EnemyPool.SpawnEnemy를 호출한다. 위치를 찾지 못하면 이번 소환을 건너뛴다.
    /// </summary>
    private void SpawnOne(SpawnKind kind)
    {
        if (kind == SpawnKind.HealPack)
        {
            if (TryFindSpawnPosition(worldDia / 4f - 1f, out Vector3 healPosition))
            {
                Instantiate(healPack, healPosition, Quaternion.identity);
            }
            return;
        }

        if (TryFindSpawnPosition(worldDia / 2f - 1f, out Vector3 spawnPosition))
        {
            enemyPool.SpawnEnemy(spawnPosition, (EnemyPool.PoolType)kind);
        }
    }

    /// <summary>
    /// 지정 반지름 고리에서 플레이어와 충분히 멀고 NavMesh 위에 있는 소환 위치를 찾는다.
    /// radius를 사용하며 최대 _maxSpawnAttempts회 재추첨하고, 성공 시 hit.position을 position에 저장하고 true를 반환한다.
    /// 모두 실패하면 position은 Vector3.zero, 반환값은 false다.
    /// </summary>
    private bool TryFindSpawnPosition(float radius, out Vector3 position)
    {
        for (int attempt = 0; attempt < _maxSpawnAttempts; attempt++)
        {
            float rad = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            Vector3 candidate = new Vector3(Mathf.Cos(rad) * radius, 0f, Mathf.Sin(rad) * radius);

            // 플레이어와 XZ 기준 너무 가까우면 다시 뽑는다.
            Vector3 flatDelta = candidate - player.transform.position;
            flatDelta.y = 0f;
            if (flatDelta.sqrMagnitude < _minPlayerDistance * _minPlayerDistance)
            {
                continue;
            }

            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, _navSampleRadius, NavMesh.AllAreas))
            {
                position = hit.position;
                return true;
            }
        }

        // 실패 경고는 판당 1회만 남긴다.
        if (!_hasWarnedSpawnFail)
        {
            _hasWarnedSpawnFail = true;
            Debug.LogWarning($"반지름 {radius:F1} 고리에서 {_maxSpawnAttempts}회 시도 만에 소환 위치를 찾지 못해 이번 소환을 건너뜁니다.");
        }
        position = Vector3.zero;
        return false;
    }

    /// <summary>
    /// 적 풀의 프리웜을 _prewarmPerFrame 단위로 프레임 분산해 진행하는 코루틴.
    /// _schedule.TotalSpawnCount(kind, _estimateDuration)을 _minPrewarm/_maxPrewarm 범위로 clamp한 수량만큼 생성한다.
    /// </summary>
    private IEnumerator PrewarmRoutine()
    {
        SpawnKind[] enemyKinds = { SpawnKind.Basic, SpawnKind.Boom, SpawnKind.NoRush, SpawnKind.NoAbsort };

        foreach (SpawnKind kind in enemyKinds)
        {
            int required = Mathf.Clamp(_schedule.TotalSpawnCount(kind, _estimateDuration), _minPrewarm, _maxPrewarm);
            int prewarmed = 0;

            while (prewarmed < required)
            {
                int batch = Mathf.Min(_prewarmPerFrame, required - prewarmed);
                enemyPool.PrewarmTo((EnemyPool.PoolType)kind, prewarmed + batch);
                prewarmed += batch;
                yield return null;
            }
        }
    }
}
