using System;
using System.IO;
using System.Text;

using UnityEditor;

using UnityEngine;

/// <summary>
/// SpawnSchedule 인스펙터. 기본 인스펙터에 CSV 내보내기/가져오기 버튼과
/// 종류별 60초 기준 총 스폰 수 요약을 추가한다.
/// </summary>
[CustomEditor(typeof(SpawnSchedule))]
public class SpawnScheduleEditor : Editor
{
    private const float SUMMARY_TIME_LIMIT = 60f;
    private const string DEFAULT_FOLDER = "Assets/Data/Spawn";

    private SpawnSchedule _schedule;

    void OnEnable()
    {
        _schedule = (SpawnSchedule)target;
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();

        if (GUILayout.Button("CSV 내보내기"))
        {
            ExportCsv();
        }

        if (GUILayout.Button("CSV 가져오기"))
        {
            ImportCsv();
        }

        using (new EditorGUI.DisabledScope(_schedule.SourceCsv == null))
        {
            if (GUILayout.Button("연결된 CSV에서 다시 읽기"))
            {
                ApplyCsv(_schedule.SourceCsv.text);
            }
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("60초 기준 종류별 총 스폰 수", EditorStyles.boldLabel);

        foreach (SpawnKind kind in Enum.GetValues(typeof(SpawnKind)))
        {
            EditorGUILayout.LabelField(kind.ToString(),
                _schedule.TotalSpawnCount(kind, SUMMARY_TIME_LIMIT).ToString());
        }
    }

    /// <summary>
    /// 현재 웨이브 목록을 ToCsv()로 직렬화해 SaveFilePanel로 받은 경로에 UTF-8로 저장한다.
    /// 저장 경로가 프로젝트 Assets 안이면 AssetDatabase.Refresh로 가져온다.
    /// </summary>
    private void ExportCsv()
    {
        string path = EditorUtility.SaveFilePanel("CSV 내보내기", DEFAULT_FOLDER, "MainSpawnSchedule", "csv");

        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        File.WriteAllText(path, _schedule.ToCsv(), new UTF8Encoding(false));

        if (IsUnderAssets(path))
        {
            AssetDatabase.Refresh();
        }
    }

    /// <summary>
    /// OpenFilePanel로 선택한 CSV 파일을 읽어 FromCsv로 웨이브 목록을 교체한다.
    /// 파일 선택이 취소되면 아무 동작도 하지 않는다.
    /// </summary>
    private void ImportCsv()
    {
        string path = EditorUtility.OpenFilePanel("CSV 가져오기", DEFAULT_FOLDER, "csv");

        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        ApplyCsv(File.ReadAllText(path));
    }

    /// <summary>
    /// csv 문자열을 FromCsv로 파싱해 성공 시 Undo 기록 후 자산을 저장하고,
    /// 실패하면 오류 목록을 대화창으로 표시한다.
    /// </summary>
    private void ApplyCsv(string csv)
    {
        Undo.RecordObject(_schedule, "Spawn Schedule CSV Import");

        if (!_schedule.FromCsv(csv, out string error))
        {
            EditorUtility.DisplayDialog("CSV 가져오기 실패", error, "확인");
            return;
        }

        EditorUtility.SetDirty(_schedule);
        AssetDatabase.SaveAssets();
    }

    /// <summary>
    /// path가 프로젝트의 Assets 폴더 안의 경로인지 판별해 반환한다.
    /// </summary>
    private bool IsUnderAssets(string path)
    {
        string fullPath = Path.GetFullPath(path).Replace('\\', '/');
        string assetsPath = Application.dataPath.Replace('\\', '/');

        return fullPath.StartsWith(assetsPath, StringComparison.OrdinalIgnoreCase);
    }
}
