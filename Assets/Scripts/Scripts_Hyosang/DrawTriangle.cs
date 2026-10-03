using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class DrawTriangle : MonoBehaviour
{
    void Start()
    {
        Mesh mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;

        // 1. 3개의 정점(Vertex) 좌표 설정
        Vector3[] vertices = new Vector3[3]
        {
            new Vector3(0, 1, 0),   // 꼭대기 점
            new Vector3(-0.5f, 0, 0), // 왼쪽 아래 점
            new Vector3(0.5f, 0, 0)   // 오른쪽 아래 점
        };

        // 2. 삼각형을 이루는 정점 인덱스 순서 (시계 방향)
        int[] triangles = new int[3] { 0, 1, 2 };

        mesh.vertices = vertices;
        mesh.triangles = triangles;

        // 3. 조명 계산 및 법선(Normal) 재계산
        mesh.RecalculateNormals();
    }
}
