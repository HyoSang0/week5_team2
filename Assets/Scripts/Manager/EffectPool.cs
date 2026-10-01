using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Pool;

/// <summary>
/// 씬 수정 없이 런타임에 이펙트 오브젝트를 풀링하는 정적 유틸.
/// 프리팹 또는 문자열 키 단위로 ObjectPool을 생성/관리하며, 풀 루트는 씬 로드 시 파괴되면 다시 만든다.
/// </summary>
public static class EffectPool
{
    private const string POOL_ROOT_NAME = "EffectPool";
    private const int MAX_POOL_SIZE = 512;

    private static Transform _poolRoot;
    private static readonly Dictionary<string, ObjectPool<GameObject>> _pools =
        new Dictionary<string, ObjectPool<GameObject>>();

    /// <summary>
    /// 도메인 리로드가 꺼진 환경에서 정적 상태를 초기화한다. 이전 씬의 죽은 참조를 남기지 않는다.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        _poolRoot = null;
        _pools.Clear();
    }

    /// <summary>
    /// 프리팹을 키로 관리되는 풀에서 이펙트를 꺼낸다.
    /// pos/rot을 적용하고 활성화된 오브젝트를 반환하며, 소속 풀 키를 PooledEffect에 기록한다.
    /// </summary>
    public static GameObject Get(GameObject prefab, Vector3 pos, Quaternion rot)
    {
        return GetInternal("P#" + prefab.GetInstanceID(), () => UnityEngine.Object.Instantiate(prefab), pos, rot);
    }

    /// <summary>
    /// 프리팹 에셋이 아닌 원본(예: 적 자식 오러)을 복제할 때 쓰는 문자열 키 기반 풀에서 이펙트를 꺼낸다.
    /// create로 원본을 복제하고 pos/rot을 적용한 활성화 오브젝트를 반환한다.
    /// </summary>
    public static GameObject Get(string key, Func<GameObject> create, Vector3 pos, Quaternion rot)
    {
        return GetInternal("K#" + key, create, pos, rot);
    }

    /// <summary>
    /// 풀 소속 오브젝트를 비활성화 후 풀에 반환한다.
    /// PooledEffect가 없거나 소속 풀이 이미 사라진 오브젝트는 Destroy로 폴백한다.
    /// </summary>
    public static void Release(GameObject go)
    {
        if (go == null)
            return;

        EnsureRoot();

        PooledEffect pooled = go.GetComponent<PooledEffect>();
        if (pooled == null || !_pools.TryGetValue(pooled.PoolKey, out ObjectPool<GameObject> pool))
        {
            UnityEngine.Object.Destroy(go);
            return;
        }

        pool.Release(go);
    }

    /// <summary>
    /// seconds초 후 자동으로 풀에 반환하도록 설정한다. PooledEffect가 없는 오브젝트는 Destroy(go, seconds)로 폴백한다.
    /// </summary>
    public static void ReleaseAfter(GameObject go, float seconds)
    {
        if (go == null)
            return;

        PooledEffect pooled = go.GetComponent<PooledEffect>();
        if (pooled == null)
        {
            UnityEngine.Object.Destroy(go, seconds);
            return;
        }

        pooled.SetAutoRelease(seconds);
    }

    /// <summary>
    /// 해당 키의 풀을 없으면 생성하고, 오브젝트를 꺼내 위치/회전과 PooledEffect의 풀 키를 지정해 반환한다.
    /// </summary>
    private static GameObject GetInternal(string poolKey, Func<GameObject> create, Vector3 pos, Quaternion rot)
    {
        EnsureRoot();

        if (!_pools.TryGetValue(poolKey, out ObjectPool<GameObject> pool))
        {
            Transform root = _poolRoot;
            pool = new ObjectPool<GameObject>(
                createFunc: () =>
                {
                    GameObject created = create();
                    created.transform.SetParent(root, false);
                    created.SetActive(false);
                    return created;
                },
                actionOnGet: obj => obj.SetActive(true),
                actionOnRelease: obj =>
                {
                    obj.SetActive(false);
                    if (_poolRoot != null)
                        obj.transform.SetParent(_poolRoot, false);
                },
                actionOnDestroy: obj => UnityEngine.Object.Destroy(obj),
                defaultCapacity: 16,
                maxSize: MAX_POOL_SIZE);
            _pools[poolKey] = pool;
        }

        GameObject result = pool.Get();
        result.transform.SetPositionAndRotation(pos, rot);

        // 원본이 비활성 상태였던 복제본도 여기서 반드시 활성화된다.
        PooledEffect pooled = result.GetComponent<PooledEffect>();
        if (pooled == null)
            pooled = result.AddComponent<PooledEffect>();
        pooled.Bind(poolKey);
        result.SetActive(true);

        return result;
    }

    /// <summary>
    /// 풀 루트가 없거나 씬 로드 등으로 파괴되어 있으면 새 루트를 만들고 죽은 풀 참조를 버린다.
    /// </summary>
    private static void EnsureRoot()
    {
        if (_poolRoot != null)
            return;

        // 루트와 함께 풀 보관 오브젝트도 이미 파괴된 상태이므로 참조만 비운다.
        _pools.Clear();

        GameObject root = new GameObject(POOL_ROOT_NAME);
        _poolRoot = root.transform;
    }
}
