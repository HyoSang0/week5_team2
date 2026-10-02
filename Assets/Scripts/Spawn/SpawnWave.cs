using System;

using UnityEngine;

/// <summary>
/// StartTime초 후부터 Duration초 동안 Interval초마다 Count마리를 스폰하는 웨이브 데이터.
/// Duration이 0 이하이면 StartTime에 1회만 스폰한다.
/// </summary>
[Serializable]
public struct SpawnWave
{
    [SerializeField] private SpawnKind _kind;
    [SerializeField] private float _startTime;
    [SerializeField] private float _interval;
    [SerializeField] private float _duration;
    [SerializeField] private int _count;

    /// <summary>웨이브가 스폰하는 대상의 종류.</summary>
    public SpawnKind Kind => _kind;

    /// <summary>스폰이 시작되는 시점(초).</summary>
    public float StartTime => _startTime;

    /// <summary>스폰 주기(초).</summary>
    public float Interval => _interval;

    /// <summary>스폰이 유지되는 시간(초). 0 이하이면 1회만 스폰.</summary>
    public float Duration => _duration;

    /// <summary>스폰 1회당 생성할 수.</summary>
    public int Count => _count;

    /// <summary>
    /// 지정한 스폰 종류와 시간 설정으로 SpawnWave를 생성한다.
    /// kind, startTime, interval, duration, count를 입력받아 각 필드에 저장한다.
    /// </summary>
    public SpawnWave(SpawnKind kind, float startTime, float interval, float duration, int count)
    {
        _kind = kind;
        _startTime = startTime;
        _interval = interval;
        _duration = duration;
        _count = count;
    }

    /// <summary>
    /// timeLimit 이내에 스폰이 발생하는 횟수를 반환한다. StartTime이 timeLimit 이상이면 0이고,
    /// Duration이 0 이하이면 1을 반환한다. 그 외에는 기존 SpawnManager 코루틴의
    /// time = duration; while(time > 0){ spawn; time -= interval } 루프와 동일한 횟수를 계산한다.
    /// </summary>
    public int SpawnTimesWithin(float timeLimit)
    {
        if (_startTime >= timeLimit)
        {
            return 0;
        }

        if (_duration <= 0f)
        {
            return 1;
        }

        int times = 0;
        float time = _duration;
        float elapsed = _startTime;

        // 기존 코루틴의 time -= interval 루프와 동일한 횟수를 유지한다.
        // interval이 0 이하면 무한 대기를 막기 위해 1회로 종료한다.
        while (time > 0f && elapsed < timeLimit)
        {
            times++;

            if (_interval <= 0f)
            {
                break;
            }

            elapsed += _interval;
            time -= _interval;
        }

        return times;
    }
}
