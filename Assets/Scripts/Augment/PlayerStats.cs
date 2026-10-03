using System;
using System.Collections.Generic;

using UnityEngine;

/// <summary>
/// 증강으로 누적된 스탯 수정자를 보관하고 기본값에 적용하는 씬 전역 단일 컴포넌트.
/// </summary>
public class PlayerStats : MonoBehaviour
{
    /// <summary>
    /// 씬에 존재하는 전역 인스턴스. 씬에 하나만 배치한다.
    /// </summary>
    public static PlayerStats Instance { get; private set; }

    // 스탯별 누적 퍼센트 합과 플랫 합을 각각 저장한다.
    private Dictionary<StatType, float> _percentSums = new Dictionary<StatType, float>();
    private Dictionary<StatType, float> _flatSums = new Dictionary<StatType, float>();

    /// <summary>
    /// 누적 스탯 수정자가 변경됐을 때 발생한다. 인자 없음.
    /// </summary>
    public event Action OnStatsChanged;

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>
    /// modifiers의 각 수정자를 스탯별 percent 합 / flat 합에 누적하고 OnStatsChanged를 1회 발생시킨다.
    /// 변경된 누적 상태를 PlayerStats에 저장한다.
    /// </summary>
    public void AddModifiers(IReadOnlyList<StatModifier> modifiers)
    {
        if (modifiers == null)
        {
            return;
        }

        foreach (StatModifier modifier in modifiers)
        {
            AccumulateValue(_percentSums, modifier.Stat, modifier.Percent);
            AccumulateValue(_flatSums, modifier.Stat, modifier.Flat);
        }

        OnStatsChanged?.Invoke();
    }

    /// <summary>
    /// stat에 누적된 수정자를 baseValue에 적용한 값을 반환한다.
    /// (baseValue + flat 합) * (1 + percent 합 / 100)으로 계산하며 결과가 음수면 0을 반환한다.
    /// </summary>
    public float Apply(StatType stat, float baseValue)
    {
        float flatSum = GetSum(_flatSums, stat);
        float percentSum = GetSum(_percentSums, stat);

        float result = (baseValue + flatSum) * (1f + percentSum / 100f);

        return result < 0f ? 0f : result;
    }

    /// <summary>
    /// stat에 누적된 수정자를 baseValue에 적용한 값을 정수로 반올림해 반환한다.
    /// Apply(float) 결과를 Mathf.RoundToInt로 변환한 값을 반환한다.
    /// </summary>
    public int ApplyInt(StatType stat, int baseValue)
    {
        return Mathf.RoundToInt(Apply(stat, (float)baseValue));
    }

    /// <summary>
    /// 누적된 percent / flat 합을 모두 초기화하고 OnStatsChanged를 발생시킨다.
    /// </summary>
    public void ResetAll()
    {
        _percentSums.Clear();
        _flatSums.Clear();

        OnStatsChanged?.Invoke();
    }

    /// <summary>
    /// key의 stat에 value를 더해서 sums 사전에 누적한다. 키가 없으면 새로 추가한다.
    /// </summary>
    private static void AccumulateValue(Dictionary<StatType, float> sums, StatType stat, float value)
    {
        if (value == 0f)
        {
            return;
        }

        sums.TryGetValue(stat, out float current);
        sums[stat] = current + value;
    }

    /// <summary>
    /// sums 사전에서 stat의 누적합을 반환한다. 없으면 0을 반환한다.
    /// </summary>
    private static float GetSum(Dictionary<StatType, float> sums, StatType stat)
    {
        sums.TryGetValue(stat, out float value);
        return value;
    }
}
