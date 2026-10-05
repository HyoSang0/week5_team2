using System.Collections;

using UnityEngine;

[CreateAssetMenu(menuName = "Augment/Effects/MomentumEffect")]
public class MomentumEffect : AugmentEffect
{
    private const int MAX_STACKS = 20;

    [Header("Momentum")]
    [SerializeField, Min(0f)] private float _speedPercentPerStack = 1f;
    [SerializeField, Min(0.01f)] private float _decayInterval = 1f;

    private PlayerController _player;
    private Coroutine _decayRoutine;
    private int _stacks;
    private float _nextDecayTime;

    // 증강 선택 시 플레이어를 찾고 이번 판의 중첩 상태를 초기화한다.
    public override void OnApply()
    {
        OnRunReset();
        _player = FindFirstObjectByType<PlayerController>();
    }

    // 흡수 성공마다 중첩을 하나 올리고, 최대 중첩에서도 감소 타이머를 다시 시작한다.
    public override void OnEnemyAbsorbed(Enemy enemy)
    {
        if (_player == null)
        {
            return;
        }

        _stacks = Mathf.Min(_stacks + 1, MAX_STACKS);
        _nextDecayTime = Time.time + Mathf.Max(0.01f, _decayInterval);
        ApplySpeedBonus();

        if (_decayRoutine == null)
        {
            _decayRoutine = _player.StartCoroutine(DecayStacks());
        }
    }

    // 새 판 시작 시 코루틴과 임시 속도 배율을 해제한다.
    public override void OnRunReset()
    {
        if (_player != null)
        {
            if (_decayRoutine != null)
            {
                _player.StopCoroutine(_decayRoutine);
            }

            _player.SetTemporaryMoveSpeedMultiplier(1f);
        }

        _player = null;
        _decayRoutine = null;
        _stacks = 0;
        _nextDecayTime = 0f;
    }

    // 마지막 흡수 후 정해진 시간이 지날 때마다 한 중첩만 감소시킨다.
    private IEnumerator DecayStacks()
    {
        while (_stacks > 0)
        {
            yield return null;

            if (Time.time < _nextDecayTime)
            {
                continue;
            }

            _stacks--;
            _nextDecayTime = Time.time + Mathf.Max(0.01f, _decayInterval);
            ApplySpeedBonus();
        }

        _decayRoutine = null;
    }

    // 현재 중첩을 기존 이동 속도 증강에 곱할 임시 배율로 변환한다.
    private void ApplySpeedBonus()
    {
        _player.SetTemporaryMoveSpeedMultiplier(1f + _stacks * _speedPercentPerStack / 100f);
    }
}
