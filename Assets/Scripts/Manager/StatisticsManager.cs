using System.Collections.Generic;

using PoolType = EnemyPool.PoolType;

/// <summary>
/// 런 단위 적 결과 통계를 집계하는 순수 C# 싱글톤이다. 씬을 다시 불러와도 유지되며, 새 런 시작 시 ResetRun으로 초기화한다.
/// </summary>
public sealed class StatisticsManager
{
    /// <summary>집계하는 적 결과의 유형이다. 전투 사망은 EnemyKill, 낙사 사망은 EnemyFall, 흡수 성공은 EnemyAbsorb다.</summary>
    public enum GameStatisticType
    {
        EnemyKill,
        EnemyFall,
        EnemyAbsorb,
    }

    private static StatisticsManager _instance;

    private readonly Dictionary<GameStatisticType, Dictionary<PoolType, int>> _counts = new();

    /// <summary>
    /// StatisticsManager의 전역 인스턴스를 반환한다. _instance를 사용하며, 처음 접근 시 새 인스턴스를 생성해 저장한다.
    /// </summary>
    public static StatisticsManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new StatisticsManager();
            }

            return _instance;
        }
    }

    /// <summary>
    /// 외부에서 인스턴스를 새로 만들지 못하도록 private으로 둔 싱글톤 생성자다. 별도 상태 초기화는 하지 않는다.
    /// </summary>
    private StatisticsManager() { }

    /// <summary>
    /// 지정한 통계 유형의 누적 수를 적 풀 타입별로 1 증가시킨다. type과 enemyType으로 _counts의 내부 사전을 갱신하며, 첫 기록인 조합은 0에서 시작한다.
    /// </summary>
    public void Record(GameStatisticType type, PoolType enemyType)
    {
        if (!_counts.TryGetValue(type, out Dictionary<PoolType, int> enemyTypeCounts))
        {
            enemyTypeCounts = new Dictionary<PoolType, int>();
            _counts[type] = enemyTypeCounts;
        }

        enemyTypeCounts[enemyType] = GetCount(type, enemyType) + 1;
    }

    /// <summary>
    /// 지정한 통계 유형의 전체 누적 수를 반환한다. 같은 유형의 모든 적 풀 타입별 누적 수를 합산해 조회하며, 아직 기록되지 않은 유형은 0을 반환한다.
    /// </summary>
    public int GetCount(GameStatisticType type)
    {
        if (!_counts.TryGetValue(type, out Dictionary<PoolType, int> enemyTypeCounts))
        {
            return 0;
        }

        int total = 0;
        foreach (int count in enemyTypeCounts.Values)
        {
            total += count;
        }

        return total;
    }

    /// <summary>
    /// 지정한 통계 유형에서 특정 적 풀 타입의 누적 수를 반환한다. type과 enemyType으로 _counts를 조회하며, 아직 기록되지 않은 조합은 0을 반환한다.
    /// </summary>
    public int GetCount(GameStatisticType type, PoolType enemyType)
    {
        return _counts.TryGetValue(type, out Dictionary<PoolType, int> enemyTypeCounts)
            && enemyTypeCounts.TryGetValue(enemyType, out int count) ? count : 0;
    }

    /// <summary>
    /// 모든 통계 누적 수를 지워 새 런을 시작한다. 풀 타입별 누적을 함께 담는 _counts를 비우며, GameManager의 실제 인스턴스가 런 시작 시 호출한다.
    /// </summary>
    public void ResetRun()
    {
        _counts.Clear();
    }
}
