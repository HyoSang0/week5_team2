using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Pool;
using static EnemyPool;

public class EnemyPool : MonoBehaviour
{
    [SerializeField] GameObject BasicEnemyPrefab;
    [SerializeField] GameObject BoomEnemyPrefab;
    [SerializeField] GameObject NoRushEnemyPrefab;
    [SerializeField] GameObject NoAbsortEnemyPrefab;

    ObjectPool<GameObject> BasicEnemyPool;
    ObjectPool<GameObject> BoomEnemyPool;
    ObjectPool<GameObject> NoRushEnemyPool;
    ObjectPool<GameObject> NoAbsortEnemyPool;

    // 풀에 보관 가능한 최대 수. 기존 maxEnemies(300)와 분리해 반납 시 즉시 파괴되지 않도록 한다.
    int maxPoolSize = 1000;


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
        BoomEnemyPool = CreatPool(PoolType.Boom);
        NoRushEnemyPool = CreatPool(PoolType.NoRush);
        NoAbsortEnemyPool = CreatPool(PoolType.NoAbsort);

        PrewarmedObject(BasicEnemyPool, 300);
        PrewarmedObject(BoomEnemyPool, 10);
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
        // 소환 위치가 NavMesh 위에 있는지 확인하고, 없으면 주변에서 가장 가까운Valid한 위치를 찾는다.
        bool hasNavPosition = NavMesh.SamplePosition(pos, out NavMeshHit navHit, 3f, NavMesh.AllAreas);
        Vector3 spawnPosition = hasNavPosition ? navHit.position : pos;
        if (!hasNavPosition)
        {
            Debug.LogWarning($"{pos} 주변 3m 안에서 NavMesh를 찾지 못해 원위치에 소환합니다.");
        }

        thisObject.transform.position = spawnPosition;
        thisObject.transform.rotation = Quaternion.identity;
        // 2. 새로운 위치에서 활성화
        thisObject.SetActive(true);

        // 3. 적 상태 초기화
        Enemy enemy = thisObject.GetComponent<Enemy>();
        enemy.Initialize(poolType, this);

        // 활성화 직후에도 NavMesh에 올라가지 못했다면 찾아둔 위치로 Warp 시도
        if (enemy.navMeshAgent != null && !enemy.navMeshAgent.isOnNavMesh)
        {
            if (hasNavPosition)
            {
                enemy.navMeshAgent.Warp(navHit.position);
            }
        }
    }
    /// <summary>
    /// 적이 죽었을 때 호출되는 함수. 적을 풀에 반환한다.
    /// </summary>
    /// <param name="obj">죽은 적의 게임 오브젝트 타입</param>
    /// <param name="poolType">죽은 적의 풀 타입</param>
    public void DieEnemy(GameObject obj, PoolType poolType)
    {
        switch (poolType)
        {
            case PoolType.Basic:
                // Debug.Log("Here");
                BasicEnemyPool.Release(obj);
                break;
            case PoolType.Boom:
                BoomEnemyPool.Release(obj);
                break;
            case PoolType.NoRush:
                NoRushEnemyPool.Release(obj);
                break;
            case PoolType.NoAbsort:
                Debug.Log("Big Guy is Dead");
                NoAbsortEnemyPool.Release(obj);
                break;
        }
    }

    ObjectPool<GameObject> CreatPool(PoolType poolType)
    {
        return new ObjectPool<GameObject>(
            createFunc: () => CreateEnemy(poolType),
            actionOnGet: enemy => GetEnemy(enemy, poolType),
            actionOnRelease: enemy => enemy.gameObject.SetActive(false),
            actionOnDestroy: enemy => Destroy(enemy),
            maxSize: maxPoolSize
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
        // enemy.SetActive(true);
        // Enemy Enemy = enemy.gameObject.GetComponent<Enemy>();
        // Enemy.Initialize(poolType, this);
    }

    // 오브젝트 풀 미리 생성해두는 함수.
    void PrewarmedObject(ObjectPool<GameObject> pool, int count)
    {
        GameObject[] prewarmedEnemy = new GameObject[count];
        for (int i = 0; i < count; i++) prewarmedEnemy[i] = pool.Get();
        for (int i = 0; i < count; i++) pool.Release(prewarmedEnemy[i]);
    }



}
