/// <summary>
/// 증강이 선택하는 흡수 영역 유형.
/// </summary>
public enum AbsorptionAreaType
{
    None = 0,
    Default = 1,

    /// <summary>
    /// 흡수를 일정 시간 유지되는 포식 모드로 전환하는 유형. 영역에 닿은 적을 즉시 흡수한다.
    /// </summary>
    Conversion = 2,
}
