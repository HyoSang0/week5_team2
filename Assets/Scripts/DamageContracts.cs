/// <summary>
/// 전투 피해가 발생한 경로를 구분해 수신자가 방어 정책과 후속 처리를 선택하도록 한다.
/// </summary>
public enum DamageKind
{
    EnemyContact,
    NoRushReflection,
    RushDirect,
    Dropkick,
    EnemyChain,
    ChainExplosion,
    BombCollector
}

/// <summary>
/// 피해원과 회복원 참조가 공통으로 만족하는 마커 계약이다.
/// </summary>
public interface ICombatSource
{
}

/// <summary>
/// 피해를 발생시킨 객체를 식별하는 마커 계약이다.
/// </summary>
public interface IDamageSource : ICombatSource
{
}

/// <summary>
/// 피해원과 회복원 참조의 유효성을 중앙에서 검사하는 공용 헬퍼다.
/// </summary>
internal static class SourceValidator
{
    /// <summary>
    /// 소스 참조가 존재하고 파괴되지 않았는지 검사한다.
    /// source로 검사할 ICombatSource 참조를 받아, null이 아니고 UnityEngine.Object라면 파괴되지 않았을 때 true를 반환한다.
    /// </summary>
    internal static bool IsValid(ICombatSource source)
    {
        if (source == null)
        {
            return false;
        }

        // UnityEngine.Object 구현은 파괴 후 fake-null이 되므로 == 비교로만 파괴를 잡아낼 수 있다.
        return source is not UnityEngine.Object unityObject || unityObject != null;
    }
}

/// <summary>
/// 전투 피해를 받을 수 있는 객체가 제공하는 단일 피해 진입점이다.
/// </summary>
public interface IDamageable
{
    /// <summary>
    /// 피해 정보를 검증하고 수신 객체의 체력 및 사망 상태에 반영한다.
    /// damageInfo는 피해량, 종류, 피해원과 즉사 여부를 제공하며, 피해가 처리되면 true를 반환한다.
    /// </summary>
    bool TakeDamage(DamageInfo damageInfo);
}

/// <summary>
/// 피해량, 유형, 발생 객체와 즉사 여부를 전달하는 불변 전투 피해 데이터다.
/// </summary>
public readonly struct DamageInfo
{
    /// <summary>
    /// 전투 피해량을 반환한다.
    /// </summary>
    public int Amount { get; }

    /// <summary>
    /// 수신자가 방어 정책을 선택할 수 있도록 피해 유형을 반환한다.
    /// </summary>
    public DamageKind Kind { get; }

    /// <summary>
    /// 피해를 발생시킨 객체를 반환한다.
    /// </summary>
    public IDamageSource Source { get; }

    /// <summary>
    /// 피해원 참조가 존재하고 파괴되지 않았는지 반환한다.
    /// Source를 중앙 유효성 검사로 확인해 UnityEngine.Object의 fake-null까지 판정하며, 유효하면 true를 반환한다.
    /// </summary>
    public bool HasValidSource => SourceValidator.IsValid(Source);

    /// <summary>
    /// 일반 피해량 대신 즉사 처리를 요청하는지 반환한다.
    /// </summary>
    public bool IsLethal { get; }

    /// <summary>
    /// 피해 요청을 구성해 이후 변경되지 않도록 저장한다.
    /// amount는 일반 피해량, kind는 공격 경로, source는 피해원, isLethal은 즉사 요청 여부다.
    /// </summary>
    public DamageInfo(int amount, DamageKind kind, IDamageSource source, bool isLethal = false)
    {
        Amount = amount;
        Kind = kind;
        Source = source;
        IsLethal = isLethal;
    }
}
