using UnityEngine;
using UnityEngine.Pool;

public class EnemyPool : MonoBehaviour
{
    ObjectPool<GameObject> enemyPool;
    [SerializeField]GameObject EnemyPrefab;
    public int maxEnemis = 20;
    GameObject[] prewarmedEnemy;


    private void Awake()
    {
        enemyPool = new ObjectPool<GameObject>(
        createFunc: CreateEnemy,
        actionOnGet: enemy => enemy.SetActive(true),
        actionOnRelease: enemy => enemy.SetActive(false),
        actionOnDestroy: enemy => Destroy(enemy),
        maxSize: maxEnemis
        );

        prewarmedEnemy = new GameObject[maxEnemis];

        for (int i = 0; i < maxEnemis; i++) prewarmedEnemy[i] = enemyPool.Get();
        for (int i = 0; i < maxEnemis; i++) enemyPool.Release(prewarmedEnemy[i]);
    }
    GameObject CreateEnemy()
    {
        GameObject temp = Instantiate(EnemyPrefab);
        temp.gameObject.SetActive(false);
        return temp;
    }

    /// <summary>
    ///  위치와 방향을 받아 적을 소환해주는 함수
    /// </summary>
    /// <param name="pos">소환될 위치</param>
    /// <param name="rot">바라볼 각도</param>
    public void SpawnEnemy(Vector3 pos, Quaternion rot)
    {
        GameObject enemy = enemyPool.Get();
        enemy.transform.position = pos;
        enemy.transform.rotation = rot;
    }

}
