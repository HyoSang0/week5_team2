using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

using UnityEngine;

/// <summary>
/// 스폰 시간표를 보관하는 ScriptableObject. CSV 문자열로 내보내고 가져올 수 있다.
/// </summary>
[CreateAssetMenu(menuName = "Spawn/Spawn Schedule")]
public class SpawnSchedule : ScriptableObject
{
    private const string CSV_HEADER = "kind,startTime,interval,duration,count";

    [Header("Wave Settings")]
    [SerializeField] private List<SpawnWave> _waves = new List<SpawnWave>();
    [SerializeField] private TextAsset _sourceCsv;

    /// <summary>웨이브 목록(읽기 전용).</summary>
    public IReadOnlyList<SpawnWave> Waves => _waves;

    /// <summary>이 스케줄을 채우는 데 사용한 출처 CSV TextAsset. 미연결이면 null.</summary>
    public TextAsset SourceCsv => _sourceCsv;

    /// <summary>
    /// timeLimit 이내에 kind에 해당하는 웨이브들이 발생시키는 총 스폰 수를 반환한다.
    /// 각 웨이브의 SpawnTimesWithin(timeLimit) * Count의 합으로 계산한다.
    /// </summary>
    public int TotalSpawnCount(SpawnKind kind, float timeLimit)
    {
        int total = 0;

        for (int i = 0; i < _waves.Count; i++)
        {
            SpawnWave wave = _waves[i];

            if (wave.Kind == kind)
            {
                total += wave.SpawnTimesWithin(timeLimit) * wave.Count;
            }
        }

        return total;
    }

    /// <summary>
    /// 현재 웨이브 목록을 CSV 문자열로 변환해 반환한다.
    /// 헤더 줄 뒤에 kind,startTime,interval,duration,count 형식의 데이터 줄을 이어 붙인다.
    /// </summary>
    public string ToCsv()
    {
        StringBuilder builder = new StringBuilder();
        builder.Append(CSV_HEADER).Append('\n');

        for (int i = 0; i < _waves.Count; i++)
        {
            SpawnWave wave = _waves[i];

            builder.Append(wave.Kind.ToString()).Append(',')
                .Append(wave.StartTime.ToString(CultureInfo.InvariantCulture)).Append(',')
                .Append(wave.Interval.ToString(CultureInfo.InvariantCulture)).Append(',')
                .Append(wave.Duration.ToString(CultureInfo.InvariantCulture)).Append(',')
                .Append(wave.Count.ToString(CultureInfo.InvariantCulture)).Append('\n');
        }

        return builder.ToString();
    }

    /// <summary>
    /// CSV 문자열을 파싱하여 모든 줄이 성공하면 _waves를 교체한다.
    /// csv를 읽고 오류 줄이 있으면 error에 "줄 번호: 이유" 목록을 채우고 false를 반환하며
    /// 기존 _waves는 변경하지 않는다. 성공하면 error는 빈 문자열이고 true를 반환한다.
    /// </summary>
    public bool FromCsv(string csv, out string error)
    {
        List<SpawnWave> parsed = new List<SpawnWave>();
        List<string> errors = new List<string>();

        string[] lines = csv.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i].Trim();

            if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal))
            {
                continue;
            }

            string[] fields = line.Split(',');

            if (fields.Length > 0 && string.Equals(fields[0].Trim(), "kind", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (TryParseWave(fields, out SpawnWave wave, out string reason))
            {
                parsed.Add(wave);
            }
            else
            {
                errors.Add($"{i + 1}: {reason}");
            }
        }

        if (errors.Count > 0)
        {
            error = string.Join("\n", errors);
            return false;
        }

        _waves = parsed;
        error = string.Empty;
        return true;
    }

    /// <summary>
    /// CSV 한 줄의 필드 배열을 파싱해 SpawnWave를 만든다.
    /// fields를 입력받고 성공하면 wave에 웨이브를 담아 true, 실패하면 reason에 원인을 담아 false를 반환한다.
    /// </summary>
    private bool TryParseWave(string[] fields, out SpawnWave wave, out string reason)
    {
        wave = default;
        reason = null;

        if (fields.Length < 4)
        {
            reason = "필드 개수 부족 (kind,startTime,interval,duration[,count])";
            return false;
        }

        if (!Enum.TryParse(fields[0].Trim(), true, out SpawnKind kind))
        {
            reason = $"알 수 없는 kind: {fields[0].Trim()}";
            return false;
        }

        if (!float.TryParse(fields[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float startTime))
        {
            reason = $"startTime 형식 오류: {fields[1].Trim()}";
            return false;
        }

        if (!float.TryParse(fields[2].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float interval))
        {
            reason = $"interval 형식 오류: {fields[2].Trim()}";
            return false;
        }

        if (!float.TryParse(fields[3].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float duration))
        {
            reason = $"duration 형식 오류: {fields[3].Trim()}";
            return false;
        }

        int count = 1;

        if (fields.Length >= 5 && !int.TryParse(fields[4].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out count))
        {
            reason = $"count 형식 오류: {fields[4].Trim()}";
            return false;
        }

        wave = new SpawnWave(kind, startTime, interval, duration, count);
        return true;
    }
}
