using UnityEngine;

public class EnemyDeathEffect : MonoBehaviour
{
    const float LIFETIME = 1f;

    float timer = 0f;

    private void OnEnable()
    {
        // 풀 재사용 시마다 생존 타이머를 초기화한다.
        timer = 0f;
    }

    // Update is called once per frame
    void Update()
    {
        //일정 시간 후 풀에 반환
        timer += Time.deltaTime;
        if(timer >= LIFETIME)
        {
            EffectPool.Release(gameObject);
        }
    }
}
