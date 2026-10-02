using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Pool;

public class EnemyPool : MonoBehaviour
{
    [SerializeField] private GameObject BasicEnemyPrefab;
    [SerializeField] private GameObject BoomEnemyPrefab;
    [SerializeField] private GameObject NoRushEnemyPrefab;
    [SerializeField] private GameObject NoAbsortEnemyPrefab;

    private ObjectPool<GameObject> BasicEnemyPool;
    private ObjectPool<GameObject> BoomEnemyPool;
    private ObjectPool<GameObject> NoRushEnemyPool;
    private ObjectPool<GameObject> NoAbsortEnemyPool;

    // 풀에 보관 가능한 최대 수. 기존 maxEnemies(300)와 분리해 반납 시 즉시 파괴되지 않도록 한다.
    private int maxPoolSize = 1000;

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
    }

    /// <summary>
    /// 지정 타입의 풀이 totalCount에 도달하도록 비활성 오브젝트를 모두 꺼낸 뒤 부족분만큼 새로 생성해 채운다.
    /// type과 totalCount를 사용하며, missing = totalCount - CountAll이 양수일 때 CountInactive + missing만큼 Get 후 모두 Release한다.
    /// missing이 0 이하이면 아무것도 하지 않는다.
    /// </summary>
    public void PrewarmTo(PoolType type, int totalCount)
    {
        ObjectPool<GameObject> pool = GetPool(type);
        int missing = totalCount - pool.CountAll;
        if (missing <= 0)
        {
            return;
        }

        // missing만 뽑으면 Get이 기존 비활성분을 재사용해 새로 생성되지 않으므로, 비활성분을 모두 먼저 꺼낸다.
        PrewarmedObject(pool, pool.CountInactive + missing);
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
                NoAbsortEnemyPool.Release(obj);
                break;
        }
    }

    /// <summary>
    /// 타입에 대응하는 오브젝트 풀을 반환한다.
    /// poolType을 사용하며 해당 타입의 ObjectPool<GameObject>을 반환한다.
    /// </summary>
    private ObjectPool<GameObject> GetPool(PoolType poolType)
    {
        switch (poolType)
        {
            case PoolType.Basic:
                return BasicEnemyPool;
            case PoolType.Boom:
                return BoomEnemyPool;
            case PoolType.NoRush:
                return NoRushEnemyPool;
            default:
                return NoAbsortEnemyPool;
        }
    }

    ObjectPool<GameObject> CreatPool(PoolType poolType)
    {
        return new ObjectPool<GameObject>(
            createFunc: () => CreateEnemy(poolType),
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

    /// <summary>
    /// 오브젝트 풀에 오브젝트를 미리 생성해 채워 넣는다.
    /// pool과 count를 사용하며, 생성 후 즉시 반납해 비활성 상태로 보관한다.
    /// </summary>
    private void PrewarmedObject(ObjectPool<GameObject> pool, int count)
    {
        GameObject[] prewarmedEnemy = new GameObject[count];
        for (int i = 0; i < count; i++) prewarmedEnemy[i] = pool.Get();
        for (int i = 0; i < count; i++) pool.Release(prewarmedEnemy[i]);
    }
}
