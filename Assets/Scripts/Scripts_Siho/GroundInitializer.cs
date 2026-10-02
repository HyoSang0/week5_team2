using System.Collections.Generic;

using UnityEngine;

public class GroundInitializer : MonoBehaviour
{
    [Header("Inspector Settings")]
    // 타일 면이 놓일 월드 높이
    [SerializeField] private float _surfaceHeight = -0.9f;

    // 타일들을 정리할 부모, 비어 있으면 Start에서 자동 생성
    [SerializeField] private Transform _tileParent;

    public GameObject cube;
    public GameObject NavObstacle;

    // 원의 반지름 (월드 좌표 기준)
    public float radius = 5f;

    // 큐브 한 칸의 크기
    public float cubeSize = 0.2f;

    public int holeCenterX = 10;
    public int holeCenterZ = 10;

    // 격자 좌표별 Cube 저장
    private Dictionary<Vector2Int, GameObject> cubes = new Dictionary<Vector2Int, GameObject>();

    void Start()
    {
        ResolveTileParent();
        CreateCircle();
    }

    /// <summary>
    /// 타일 부모가 할당되어 있지 않으면 GroundInitializer의 자식으로
    /// "GroundTiles" GameObject를 만들어 _tileParent에 저장하고 반환한다.
    /// </summary>
    private Transform ResolveTileParent()
    {
        if (_tileParent != null)
        {
            return _tileParent;
        }

        GameObject tileRoot = new GameObject("GroundTiles");
        tileRoot.transform.SetParent(transform, false);
        _tileParent = tileRoot.transform;

        return _tileParent;
    }

    /// <summary>
    /// radius와 cubeSize 격자로 원 영역 안의 타일을 _surfaceHeight 높이에
    /// _tileParent 자식으로 생성하고 cubes 딕셔너리에 격자 좌표로 저장한다.
    /// </summary>
    private void CreateCircle()
    {
        int gridRadius = Mathf.CeilToInt(radius / cubeSize);

        for (int x = -gridRadius; x <= gridRadius; x++)
        {
            for (int z = -gridRadius; z <= gridRadius; z++)
            {
                float worldX = x * cubeSize;
                float worldZ = z * cubeSize;

                float distanceSquared = worldX * worldX + worldZ * worldZ;

                if (distanceSquared <= radius * radius)
                {
                    Vector3 position = new Vector3(worldX, _surfaceHeight, worldZ);

                    GameObject obj = Instantiate(cube, position, cube.transform.rotation, _tileParent);

                    cubes.Add(new Vector2Int(x, z), obj);
                }
            }
        }
    }

    /// <summary>
    /// centerX, centerZ 중심으로 effectRadius 범위의 타일에 10초 붕괴를 요구하고
    /// 자리에 NavObstacle를 생성해 10초 후 제거한다.
    /// </summary>
    public void DisableWave1(
        float centerX,
        float centerZ,
        float effectRadius)
    {
        int localX = Mathf.RoundToInt(centerX / cubeSize);
        int localZ = Mathf.RoundToInt(centerZ / cubeSize);
        int localRadius = Mathf.CeilToInt(effectRadius / cubeSize);

        for (int x = localX - localRadius; x <= localX + localRadius; x++)
        {
            for (int z = localZ - localRadius; z <= localZ + localRadius; z++)
            {
                if ((x - localX) * (x - localX) + (z - localZ) * (z - localZ) <= localRadius * localRadius)
                {
                    Vector2Int key = new Vector2Int(x, z);

                    if (cubes.TryGetValue(key, out GameObject obj))
                    {
                        obj.GetComponent<GroundDisableTimer>().DisableFor10Seconds();
                    }
                }

            }
        }
        GameObject obstacle = Instantiate(NavObstacle, new Vector3(centerX, 0f, centerZ), Quaternion.identity);
        obstacle.GetComponent<DestroyObstacle>().SetRadius(effectRadius);
        Destroy(obstacle, 10f);
    }
}
