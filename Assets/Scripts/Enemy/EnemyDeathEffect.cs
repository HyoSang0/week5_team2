using UnityEngine;

public class EnemyDeathEffect : MonoBehaviour
{
    EnemyPool enemyPool;
    float timer = 0f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        enemyPool = GetComponentInParent<EnemyPool>();
    }

    // Update is called once per frame
    void Update()
    {
        //일정 시간 후 파괴
        timer += Time.deltaTime;
        if(timer >= 1f)
        {
            Destroy(gameObject);
            //enemyPool.Release(this.gameObject);
        }
    }
}
