using UnityEngine;

/// <summary>
/// 가로본능 증강이 선택하는 직사각형 흡수 영역.
/// 플레이어가 바라보는 방향으로 놓이며, 차지 시간에 비례해 플레이어 앞쪽으로 길이가 area_radus_min에서 area_radus_max까지 늘어난다.
/// 흡수 키를 놓으면 기본 영역과 같이 비활성화 시점에 영역 안의 적을 한 번에 흡수한다.
/// </summary>
[DefaultExecutionOrder(100)]
public class AbsortionLineArea_Sejin : AbsortionArea_Sejin
{
    [Header("Line Area")]
    // 직사각형 영역의 폭(로컬 X 크기). 길이는 area_radus로 늘어난다.
    [SerializeField, Min(0.1f)] private float _lineWidth = 1.5f;

    private Vector3 _baseLocalPosition;
    private bool _hasBaseLocalPosition;

    /// <summary>
    /// 현재 길이 area_radus로 직사각형 영역의 크기와 위치를 갱신한다.
    /// 폭은 _lineWidth, 두께는 originScale.y로 고정하고, 길이는 로컬 Z 방향으로 설정한다.
    /// 영역의 뒤쪽 끝이 플레이어 위치에 오도록 중심을 길이의 절반만큼 앞쪽으로 옮기며, 회전은 부모(플레이어 방향)를 따른다.
    /// </summary>
    protected override void ApplyAreaScale()
    {
        // 첫 호출 시점의 위치를 플레이어 기준점으로 삼아 이후 앞쪽 이동량을 더한다.
        if (!_hasBaseLocalPosition)
        {
            _baseLocalPosition = transform.localPosition;
            _hasBaseLocalPosition = true;
        }

        transform.localRotation = Quaternion.identity;
        transform.localScale = new Vector3(_lineWidth, originScale.y, area_radus);
        transform.localPosition = _baseLocalPosition + new Vector3(0f, 0f, area_radus * 0.5f);
    }

    /// <summary>
    /// 적이 현재 직사각형 영역 안에 들어왔는지 판정한다.
    /// enemy의 위치를 영역 로컬 좌표로 변환하며, 중심점이 단위 박스의 XZ 범위 안에 있으면 true를 반환한다.
    /// </summary>
    protected override bool IsInAbsorbRange(Enemy enemy)
    {
        Vector3 localPosition = transform.InverseTransformPoint(enemy.transform.position);
        return Mathf.Abs(localPosition.x) <= 0.5f && Mathf.Abs(localPosition.z) <= 0.5f;
    }
}
