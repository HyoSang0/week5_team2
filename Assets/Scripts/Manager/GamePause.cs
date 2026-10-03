using System.Collections.Generic;

using UnityEngine;

/// <summary>
/// 일시정지 원인을 집계해 Time.timeScale을 관리하는 정적 클래스.
/// 원인이 하나 이상 남아 있으면 일시정지 상태로 간주한다.
/// </summary>
public static class GamePause
{
    private static readonly HashSet<PauseReason> _reasons = new HashSet<PauseReason>();

    /// <summary>
    /// 남아 있는 일시정지 원인이 하나 이상인지 나타낸다.
    /// </summary>
    public static bool IsPaused => _reasons.Count > 0;

    /// <summary>
    /// 일시정지 원인 reason을 추가하고 Time.timeScale을 0으로 만든다.
    /// 같은 원인이 이미 있으면 집합에 변화는 없지만 일시정지 상태는 유지된다.
    /// </summary>
    public static void Pause(PauseReason reason)
    {
        _reasons.Add(reason);
        Time.timeScale = 0f;
    }

    /// <summary>
    /// 일시정지 원인 reason을 제거하고, 남은 원인이 없으면 Time.timeScale을 1로 복원한다.
    /// </summary>
    public static void Resume(PauseReason reason)
    {
        _reasons.Remove(reason);
        if (!IsPaused)
        {
            Time.timeScale = 1f;
        }
    }

    /// <summary>
    /// 모든 일시정지 원인을 비우고 Time.timeScale을 1로 복원한다.
    /// </summary>
    public static void ResetAll()
    {
        _reasons.Clear();
        Time.timeScale = 1f;
    }

    /// <summary>
    /// 도메인 리로드 비활성화 환경에서도 런 시작 시 원인 집합이 비어 있도록 초기화한다.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Initialize()
    {
        _reasons.Clear();
    }
}
