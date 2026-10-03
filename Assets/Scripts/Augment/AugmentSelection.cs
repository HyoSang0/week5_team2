using UnityEngine;

/// <summary>
/// 증강 선택 UI의 열림 상태를 공유하는 전역 상태. 열려 있는 동안 플레이어 입력 차단 등에 사용한다.
/// </summary>
public static class AugmentSelection
{
    /// <summary>
    /// 증강 선택 UI가 열려 있는지 나타낸다. UI 열림/닫힘 시 true/false로 설정한다.
    /// </summary>
    public static bool IsOpen { get; set; }

    /// <summary>
    /// 도메인 리로드 비활성화 환경에서도 런 시작 시 IsOpen이 false로 유지되도록 초기화한다.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Initialize()
    {
        IsOpen = false;
    }
}
