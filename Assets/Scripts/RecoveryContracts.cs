/// <summary>
/// 체력 회복이 발생한 경로를 구분해 수신자가 회복 정책과 후속 처리를 선택하도록 한다.
/// </summary>
public enum HealingKind
{
    HealPack,
    ChainHeal,
    Gluttony
}

/// <summary>
/// 체력을 회복시킨 객체를 식별하는 마커 계약이다.
/// </summary>
public interface IHealingSource
{
}

/// <summary>
/// 체력을 회복받을 수 있는 객체가 제공하는 단일 회복 진입점이다.
/// </summary>
public interface IHealable
{
    /// <summary>
    /// 회복 정보를 검증하고 수신 객체의 체력에 반영한다.
    /// healingInfo는 회복량, 종류, 회복원을 제공하며, 실제로 체력이 증가했을 때만 true를 반환한다.
    /// </summary>
    bool ReceiveHealing(HealingInfo healingInfo);
}

/// <summary>
/// 회복량, 유형, 회복원을 전달하는 불변 체력 회복 데이터다.
/// </summary>
public readonly struct HealingInfo
{
    /// <summary>
    /// 체력 회복량을 반환한다.
    /// </summary>
    public int Amount { get; }

    /// <summary>
    /// 수신자가 회복 정책을 선택할 수 있도록 회복 유형을 반환한다.
    /// </summary>
    public HealingKind Kind { get; }

    /// <summary>
    /// 체력을 회복시킨 객체를 반환한다.
    /// </summary>
    public IHealingSource Source { get; }

    /// <summary>
    /// 회복원 참조가 존재하는지 반환한다.
    /// Source를 null과 비교해 참조 부재를 판정하며, 존재하면 true를 반환한다.
    /// </summary>
    public bool HasSource => Source != null;

    /// <summary>
    /// 회복 요청을 구성해 이후 변경되지 않도록 저장한다.
    /// amount는 회복량, kind는 회복 경로, source는 회복원이다.
    /// </summary>
    public HealingInfo(int amount, HealingKind kind, IHealingSource source)
    {
        Amount = amount;
        Kind = kind;
        Source = source;
    }
}
