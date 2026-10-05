using UnityEngine;

/// <summary>
/// 증강의 커스텀 런타임 동작을 정의하는 ScriptableObject 기반 추상 클래스.
/// 각 콜백은 해당 게임 이벤트 발생 시 AugmentSystem 측에서 호출한다.
/// </summary>
public abstract class AugmentEffect : ScriptableObject
{
    /// <summary>
    /// 증강이 선택된 즉시 1회 호출된다. 기본 구현은 아무 동작도 하지 않는다.
    /// </summary>
    public virtual void OnApply() { }

    /// <summary>
    /// 적이 처치됐을 때 호출된다. enemy는 처치된 적이며, 기본 구현은 아무 동작도 하지 않는다.
    /// </summary>
    public virtual void OnEnemyKilled(Enemy enemy) { }

    /// <summary>
    /// 적이 흡수됐을 때 호출된다. enemy는 흡수된 적이며, 기본 구현은 아무 동작도 하지 않는다.
    /// </summary>
    public virtual void OnEnemyAbsorbed(Enemy enemy) { }

    /// <summary>
    /// 점수가 올랐을 때 호출된다. score은 점수며, 기본 구현은 아무 동작도 하지 않는다.
    /// </summary>
    public virtual void OnScoreIncreased(float score) { }

    /// <summary>
    /// 새 판 시작 시 호출되어 Shared ScriptableObject에 남은 런타임 상태를 초기화한다.
    /// 기본 구현은 아무 동작도 하지 않는다.
    /// </summary>
    public virtual void OnRunReset() { }
}
