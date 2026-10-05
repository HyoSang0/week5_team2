using UnityEngine;

/// <summary>
/// 가로본능 증강이 선택하는 플레이어 중심 가로 일직선 흡수 영역.
/// 길이는 area_radus_min에서 area_radus_max까지 speed로 늘어나고 폭은 고정된다.
/// </summary>
[DefaultExecutionOrder(100)]
public class AbsortionLineArea_Sejin : AbsortionArea_Sejin
{
    // 일직선 영역의 고정 폭(월드 단위). 길이만 area_radus로 늘어난다.
    private const float LINE_WIDTH = 1f;

    /// <summary>
    /// 일직선 영역의 현재 길이를 transform.localScale에 반영한다.
    /// area_radus와 originScale을 사용하며, X는 길이, Y는 기존 평판과 같은 비율 두께, Z는 고정 폭으로 설정한다.
    /// 플레이어의 방향 갱신 이후 월드 회전을 초기화해 가로 방향을 유지한다.
    /// </summary>
    protected override void ApplyAreaScale()
    {
        transform.rotation = Quaternion.identity;
        transform.localScale = new Vector3(area_radus, originScale.y * area_radus, LINE_WIDTH);
    }

    /// <summary>
    /// 적이 현재 일직선 영역 범위 안에 들어왔는지 판정한다.
    /// enemy의 위치를 영역 로컬 좌표로 변환하며,
    /// 중심점이 트리거 박스의 XZ 범위 안에 있으면 true를 반환한다.
    /// </summary>
    protected override bool IsInAbsorbRange(Enemy enemy)
    {
        Vector3 localPosition = transform.InverseTransformPoint(enemy.transform.position);
        return Mathf.Abs(localPosition.x) <= 0.5f && Mathf.Abs(localPosition.z) <= 0.5f;
    }
}
