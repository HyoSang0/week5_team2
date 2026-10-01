using UnityEngine;

/// <summary>
/// EffectPool로 생성된 오브젝트에 붙는 컴포넌트.
/// 소속 풀의 키를 기억하고, 자동 반환이 설정되면 타이머(Update 기준, timeScale 영향)로 풀에 반환한다.
/// </summary>
public class PooledEffect : MonoBehaviour
{
    private const float NO_AUTO_RELEASE = -1f;

    // 소속 풀의 키. EffectPool.Get에서 기록된다.
    private string _poolKey = string.Empty;
    // 자동 반환까지의 시간. 음수면 자동 반환 없음.
    private float _duration = NO_AUTO_RELEASE;
    private float _timer = 0f;

    public string PoolKey => _poolKey;

    /// <summary>
    /// 오브젝트가 소속된 풀의 키를 기록한다. EffectPool.Get 시 호출된다.
    /// </summary>
    public void Bind(string poolKey)
    {
        _poolKey = poolKey;
    }

    /// <summary>
    /// seconds초 후 풀에 자동 반환되도록 설정하고 타이머를 시작 위치로 되돌린다.
    /// </summary>
    public void SetAutoRelease(float seconds)
    {
        _duration = seconds;
        _timer = 0f;
    }

    private void OnEnable()
    {
        // 재사용 시마다 타이머를 초기화한다.
        _timer = 0f;
    }

    private void Update()
    {
        if (_duration < 0f)
            return;

        _timer += Time.deltaTime;
        if (_timer >= _duration)
            EffectPool.Release(gameObject);
    }
}
