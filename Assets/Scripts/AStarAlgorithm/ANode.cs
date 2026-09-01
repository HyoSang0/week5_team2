using UnityEngine;

public class ANode : MonoBehaviour
{
    public bool isWalkAble;
    public Vector3 worldPos;
    public int gridX;
    public int gridY;

    public int gCost; // 시작 노드에서 현재 노드까지의 비용
    public int hCost; // 현재 노드에서 목표 노드까지의 비용
    public ANode parentNode; // 경로를 추적하기 위한 부모 노드

    public ANode(bool nWalkable, Vector3 nWorldPos, int nGridX, int nGridY)
    {
        isWalkAble = nWalkable;
        worldPos = nWorldPos;
        gridX = nGridX;
        gridY = nGridY;
    }

    public int fCost
    {
        get{ return gCost + hCost; }
    }
}
