using UnityEngine;
using UnityEngine.Pool;

public class EnemyPool : MonoBehaviour
{
    [SerializeField]GameObject BasicEnemyPrefab;
    [SerializeField] GameObject BoomEnemyPrefab;
    [SerializeField] GameObject NoRushEnemyPrefab;
    [SerializeField] GameObject NoAbsortEnemyPrefab;

    ObjectPool<GameObject> BasicEnemyPool;
    ObjectPool<GameObject> BoomEnemyPool;
    ObjectPool<GameObject> NoRushEnemyPool;
    ObjectPool<GameObject> NoAbsortEnemyPool;

    int maxEnemies = 300;


    // 적들을 한 번에 관리하기 위한 enum 타입.
    public enum PoolType
    {
        Basic,
        Boom,
        NoRush,
        NoAbsort
    }



    private void Awake()
    {
        BasicEnemyPool = CreatPool(PoolType.Basic);
        //BoomEnemyPool = CreatPool(PoolType.Boom);
        NoRushEnemyPool = CreatPool(PoolType.NoRush);
        NoAbsortEnemyPool = CreatPool(PoolType.NoAbsort);

        PrewarmedObject(BasicEnemyPool, 300);
        //PrewarmedObject(BoomEnemyPool, 10);
        PrewarmedObject(NoRushEnemyPool, 10);
        PrewarmedObject(NoAbsortEnemyPool, 10);
    }
    /// <summary>
    ///  위치와 방향을 받아 적을 소환해주는 함수
    /// </summary>
    /// <param name="pos">소환될 위치</param>
    /// <param name="rot">바라볼 각도</param>
    /// <param name="poolType">소환할 적의 타입</param>
    public void SpawnEnemy(Vector3 pos, PoolType poolType)
    {
        GameObject thisObject = null;

        switch (poolType)
        {
            case PoolType.Basic:
                thisObject = BasicEnemyPool.Get();
                break;
            case PoolType.Boom:
                thisObject = BoomEnemyPool.Get();
                break;
            case PoolType.NoRush:
                thisObject = NoRushEnemyPool.Get();
                break;
            case PoolType.NoAbsort:
                thisObject = NoAbsortEnemyPool.Get();
                break;
        }
        thisObject.transform.position = pos;
        thisObject.transform.rotation = new Quaternion();
    }
    /// <summary>
    /// 적이 죽었을 때 호출되는 함수. 적을 풀에 반환한다.
    /// </summary>
    /// <param name="obj">죽은 적의 게임 오브젝트 타입</param>
    public void DieEnemy(GameObject obj)
    {
        BasicEnemyPool.Release(obj);
    }

    ObjectPool<GameObject> CreatPool(PoolType poolType)
    {
        return new ObjectPool<GameObject>(
            createFunc: () => CreateEnemy(poolType),
            actionOnGet: enemy => GetEnemy(enemy, poolType),
            actionOnRelease: enemy => enemy.gameObject.SetActive(false),
            actionOnDestroy: enemy => Destroy(enemy),
            maxSize: maxEnemies
            );
    }
    GameObject CreateEnemy(PoolType poolType)
    {
        GameObject thisObject = null;
        switch (poolType)
        {
            case PoolType.Basic:
                thisObject = Instantiate(BasicEnemyPrefab);
                break;
            case PoolType.Boom:
                thisObject = Instantiate(BoomEnemyPrefab);
                break;
            case PoolType.NoRush:
                thisObject = Instantiate(NoRushEnemyPrefab);
                break;
            case PoolType.NoAbsort:
                thisObject = Instantiate(NoAbsortEnemyPrefab);
                break;
        }

        thisObject.transform.SetParent(transform);
        thisObject.gameObject.SetActive(false);
        return thisObject;
    }

    void GetEnemy(GameObject enemy, PoolType poolType)
    {
        enemy.SetActive(true);
        Enemy Enemy = enemy.gameObject.GetComponent<Enemy>();
        Enemy.Initialize(poolType); // ToDo: Enemy에서 PoolType에 따라 초기화 해주는 함수 제작
    }

    // 오브젝트 풀 미리 생성해두는 함수.
    void PrewarmedObject(ObjectPool<GameObject> pool, int count)
    {
        GameObject[] prewarmedEnemy = new GameObject[count];
        for (int i = 0; i < count; i++) prewarmedEnemy[i] = pool.Get();
        for (int i = 0; i < count; i++) pool.Release(prewarmedEnemy[i]);
    }



}
