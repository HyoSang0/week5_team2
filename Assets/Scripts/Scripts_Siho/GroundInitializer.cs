using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Collections;

public class GroundInitializer : MonoBehaviour
{
    public GameObject cube;
    public GameObject NavObstacle;

    // 원의 반지름 (월드 좌표 기준)
    public float radius = 5f;

    // 큐브 한 칸의 크기
    public float cubeSize = 0.2f;

    //public int holeCenterX = 10;
    //public int holeCenterZ = 10;

    // 격자 좌표별 Cube 저장
    private Dictionary<Vector2Int, GameObject> cubes = new Dictionary<Vector2Int, GameObject>();
    void Start()
    {
        CreateCircle();
    }
    void CreateCircle()
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
                    Vector3 position = new Vector3(worldX, -1f, worldZ);

                    GameObject obj = Instantiate(cube, position, Quaternion.identity);

                    cubes.Add(new Vector2Int(x, z), obj);
                }
            }
        }
    }
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