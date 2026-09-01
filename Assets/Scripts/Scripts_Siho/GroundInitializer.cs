using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class GroundInitializer : MonoBehaviour
{
    public GameObject cube;

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
        CreateCircle();
    }
 void Update()
    {
        // New Input System으로 Space바 입력
        if (Keyboard.current != null &&
            Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            Disable30x30(holeCenterX, holeCenterZ);
        }
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

                float distanceSquared =
                    worldX * worldX +
                    worldZ * worldZ;

                if (distanceSquared <= radius * radius)
                {
                    Vector3 position = new Vector3(
                        worldX,
                        -1f,
                        worldZ
                    );

                    GameObject obj = Instantiate(
                        cube,
                        position,
                        Quaternion.identity
                    );

                    cubes.Add(
                        new Vector2Int(x, z),
                        obj
                    );
                }
            }
        }
        }

    public void Disable30x30(int centerX, int centerZ)
    {
        const int size = 30;
        const int half = size / 2;

        int startX = centerX - half;
        int startZ = centerZ - half;

        for (int x = startX; x < startX + size; x++)
        {
            for (int z = startZ; z < startZ + size; z++)
            {
                Vector2Int key = new Vector2Int(x, z);

                if (cubes.TryGetValue(key, out GameObject obj))
                {
                    obj.SetActive(false);
                }
            }
        }
    }
}