using System;

using UnityEngine;

/// <summary>
/// 증강 효과가 구독할 수 있는 전역 적 이벤트(처치/흡수)를 중개하는 정적 클래스.
/// </summary>
public static class AugmentEvents
{
    /// <summary>
    /// 적이 처치됐을 때 발생하며 처치된 enemy를 인자로 전달한다.
    /// </summary>
    public static event Action<Enemy> OnEnemyKilled;

    /// <summary>
    /// 적이 흡수됐을 때 발생하며 흡수된 enemy를 인자로 전달한다.
    /// </summary>
    public static event Action<Enemy> OnEnemyAbsorbed;

    /// <summary>
    /// 적 처치 이벤트를 발생시켜 구독 중인 증강 효과에 enemy를 전달한다.
    /// </summary>
    public static void RaiseEnemyKilled(Enemy enemy)
    {
        OnEnemyKilled?.Invoke(enemy);
    }

    /// <summary>
    /// 적 흡수 이벤트를 발생시켜 구독 중인 증강 효과에 enemy를 전달한다.
    /// </summary>
    public static void RaiseEnemyAbsorbed(Enemy enemy)
    {
        OnEnemyAbsorbed?.Invoke(enemy);
    }

    /// <summary>
    /// 두 적 이벤트의 모든 구독을 제거한다.
    /// </summary>
    public static void ClearAll()
    {
        OnEnemyKilled = null;
        OnEnemyAbsorbed = null;
    }

    /// <summary>
    /// 도메인 리로드 비활성화 환경에서도 정적 이벤트 구독이 남지 않도록 초기화한다.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Initialize()
    {
        ClearAll();
    }
}
