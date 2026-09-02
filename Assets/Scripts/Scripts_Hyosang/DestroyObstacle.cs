using UnityEngine;
using UnityEngine.AI;

public class DestroyObstacle : MonoBehaviour
{
    public NavMeshObstacle navMeshObstacle;
    [Header("Enemy 접근 불가 구역 크기 설정.")]
    public float destroyRadius = 5f;
    public float destroyHeight = 10f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        navMeshObstacle = GetComponent<NavMeshObstacle>();
        navMeshObstacle.radius = destroyRadius;
        navMeshObstacle.height = destroyHeight;
    }

    public void SetRadius(float radius)
    {
        destroyRadius = radius;
        navMeshObstacle.radius = radius;
    }
}
