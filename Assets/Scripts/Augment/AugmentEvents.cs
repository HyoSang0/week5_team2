using System;

using UnityEngine;

/// <summary>
/// 증강 효과와 흡수 영역 선택자가 구독하는 전역 이벤트를 중개하는 정적 클래스.
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
    /// 흡수 활성화가 시작됐을 때 발생한다.
    /// </summary>
    public static event Action OnAbsorptionStarted;

    /// <summary>
    /// 흡수 활성화가 종료됐을 때 발생한다.
    /// 홀드 모드의 종료 시점 일괄 흡수가 모두 집계된 뒤 발생한다.
    /// </summary>
    public static event Action OnAbsorptionEnded;

    /// <summary>
    /// 흡수 영역 유형 선택이 변경됐을 때 발생하며 선택된 type을 전달한다.
    /// None 선택은 발생하지 않는다.
    /// </summary>
    public static event Action<AbsorptionAreaType> OnAbsorptionAreaTypeSelected;

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
    /// 흡수 활성화 시작 이벤트를 발생시켜 구독 중인 증강 효과에 활성화 시작을 전달한다.
    /// </summary>
    public static void RaiseAbsorptionStarted()
    {
        OnAbsorptionStarted?.Invoke();
    }

    /// <summary>
    /// 흡수 활성화 종료 이벤트를 발생시켜 구독 중인 증강 효과에 활성화 종료를 전달한다.
    /// </summary>
    public static void RaiseAbsorptionEnded()
    {
        OnAbsorptionEnded?.Invoke();
    }

    /// <summary>
    /// 흡수 영역 유형 선택 이벤트를 발생시킨다.
    /// type이 None이면 구독자의 현재 선택을 건드리지 않도록 발생시키지 않는다.
    /// </summary>
    public static void RaiseAbsorptionAreaTypeSelected(AbsorptionAreaType type)
    {
        if (type == AbsorptionAreaType.None)
        {
            return;
        }

        OnAbsorptionAreaTypeSelected?.Invoke(type);
    }

    /// <summary>
    /// 적 처치·흡수, 흡수 활성화 시작·종료, 흡수 영역 선택 이벤트의 모든 구독을 제거한다.
    /// </summary>
    public static void ClearAll()
    {
        OnEnemyKilled = null;
        OnEnemyAbsorbed = null;
        OnAbsorptionStarted = null;
        OnAbsorptionEnded = null;
        OnAbsorptionAreaTypeSelected = null;
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
