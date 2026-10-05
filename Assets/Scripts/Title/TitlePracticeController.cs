using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.AI;

using TMPro;

/// <summary>타이틀의 고정 위치에서 기본 적과 특수 적 연습을 번갈아 진행한다.</summary>
public class TitlePracticeController : MonoBehaviour
{
    private const float RETRY_SECONDS = 0.5f;

    [Header("References")]
    [SerializeField] private EnemyPool _enemyPool;
    [SerializeField] private GroundInitializer _ground;
    [SerializeField] private Transform _player;
    [SerializeField] private Transform[] _spawnPoints;
    [SerializeField] private TMP_Text _statusText;

    [Header("Practice")]
    [SerializeField, Min(0f)] private float _waveDelay = 1.5f;
    [SerializeField, Min(0.1f)] private float _supportRadius = 0.6f;
    [SerializeField, Min(0f)] private float _playerClearance = 2f;
    private readonly HashSet<Enemy> _activeEnemies = new HashSet<Enemy>();
    private readonly List<Vector3> _positions = new List<Vector3>(4);
    private bool _specialWave;

    void OnEnable()
    {
        _enemyPool.EnemyReturned += HandleEnemyReturned;
    }

    void OnDisable()
    {
        _enemyPool.EnemyReturned -= HandleEnemyReturned;
    }

    IEnumerator Start()
    {
        // 지면 생성과 적 풀의 Start 순서에 의존하지 않도록 한 프레임 기다린다.
        yield return null;
        WaitForSeconds retry = new WaitForSeconds(RETRY_SECONDS);
        WaitForSeconds delay = new WaitForSeconds(_waveDelay);

        while (true)
        {
            int count = _specialWave ? 3 : 4;
            _statusText.text = "연습 준비 중 · 지면 복구를 기다려주세요";
            while (!TryCollectPositions(count))
            {
                yield return retry;
            }

            for (int i = 0; i < count; i++)
            {
                EnemyPool.PoolType kind = _specialWave
                    ? (EnemyPool.PoolType)(i + 1)
                    : EnemyPool.PoolType.Basic;
                Enemy enemy = _enemyPool.SpawnEnemy(_positions[i], kind);
                _activeEnemies.Add(enemy);
            }

            UpdateStatus();
            while (_activeEnemies.Count > 0)
            {
                yield return retry;
            }

            _statusText.text = "모두 처치했습니다! · 다음 연습 준비 중";
            yield return delay;
            _specialWave = !_specialWave;
        }
    }

    /// <summary>
    /// 고정된 후보 위치에서 이번 그룹에 필요한 안전한 위치만 수집한다.
    /// count와 지면·플레이어 거리 설정을 사용하며, _positions를 채우면 true를 반환한다.
    /// </summary>
    private bool TryCollectPositions(int count)
    {
        _positions.Clear();
        // 폭발 경고가 끝나기 전에 다음 적을 같은 위치에 배치하지 않는다.
        if (FindAnyObjectByType<GroundWarning>() != null)
        {
            return false;
        }

        foreach (Transform point in _spawnPoints)
        {
            if (!NavMesh.SamplePosition(point.position, out NavMeshHit hit, 1f, NavMesh.AllAreas)
                || !_ground.HasPracticeGround(hit.position, _supportRadius))
            {
                continue;
            }

            Vector3 distance = hit.position - _player.position;
            distance.y = 0f;
            if (distance.sqrMagnitude < _playerClearance * _playerClearance)
            {
                continue;
            }

            _positions.Add(hit.position);
            if (_positions.Count == count)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 사망 연출·흡수·낙사 후 풀로 반환된 연습 적을 남은 수에서 제외한다.
    /// enemy가 현재 그룹에 속하면 집합과 안내 문구를 갱신한다.
    /// </summary>
    private void HandleEnemyReturned(Enemy enemy)
    {
        if (_activeEnemies.Remove(enemy))
        {
            UpdateStatus();
        }
    }

    /// <summary>
    /// 현재 그룹과 남은 적 수를 연습 안내에 표시한다.
    /// _specialWave와 _activeEnemies를 읽어 _statusText를 갱신한다.
    /// </summary>
    private void UpdateStatus()
    {
        string label = _specialWave ? "특수 적 연습" : "기본 적 연습";
        _statusText.text = $"{label} · 남은 적 {_activeEnemies.Count}마리";
    }
}
