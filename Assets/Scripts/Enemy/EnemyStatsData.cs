using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// 풀 타입별 적 밸런스 정의를 담는 ScriptableObject이다.
/// maxHealth, moveSpeed, contactDamage를 정의하며 Enemy.Initialize가 매 스폰·풀 재사용마다 적에게 적용한다.
/// </summary>
[CreateAssetMenu(fileName = "EnemyStats", menuName = "Game/Enemy Stats Data")]
public class EnemyStatsData : ScriptableObject
{
    [FormerlySerializedAs("maxHealth")]
    [SerializeField] private int _maxHealth;

    [FormerlySerializedAs("moveSpeed")]
    [SerializeField] private float _moveSpeed;

    [FormerlySerializedAs("contactDamage")]
    [SerializeField] private int _contactDamage;

    /// <summary>스폰 시 적에게 적용되는 최대 체력이다.</summary>
    public int MaxHealth => _maxHealth;

    /// <summary>스폰 시 적에게 적용되는 이동 속도이다.</summary>
    public float MoveSpeed => _moveSpeed;

    /// <summary>플레이어 접촉 시 가해지는 접촉 피해량이다.</summary>
    public int ContactDamage => _contactDamage;
}
