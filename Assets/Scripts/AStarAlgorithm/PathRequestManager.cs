using System;
using System.Collections.Generic;

using UnityEngine;

public class PathRequestManager : MonoBehaviour
{
    Queue<PathRequest> pathRequestQueue = new Queue<PathRequest>();
    PathRequest currentPathRequest;

    static PathRequestManager instance;
    PathFinding pathFinding;

    bool isProcessingPath;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        instance = this;
        pathFinding = GetComponent<PathFinding>();
    }

    /// <summary>
    /// 경로 탐색 요청을 받아 콜백과 함께 요청 큐에 등록하고 처리를 시도한다.
    /// pathStart, pathEnd는 경로의 시작·끝 월드 좌표이고 callback은 탐색 결과를 받을 델리게이트다.
    /// pathRequestQueue에 새 요청을 추가한다.
    /// </summary>
    public static void RequestPath(Vector3 pathStart, Vector3 pathEnd, Action<Vector3[], bool> callback)
    {
        PathRequest newRequest = new PathRequest(pathStart, pathEnd, callback);
        instance.pathRequestQueue.Enqueue(newRequest);
        instance.TryProcessNext();
    }

    void TryProcessNext()
    {
        if(!isProcessingPath && pathRequestQueue.Count > 0)
        {
            currentPathRequest = pathRequestQueue.Dequeue();
            isProcessingPath = true;
            pathFinding.StartFindPath(currentPathRequest.pathStart, currentPathRequest.pathEnd);
        }
    }

    public void FinishedProcessingPath(Vector3[] path, bool success)
    {
        currentPathRequest.callback(path, success);
        isProcessingPath = false;
        TryProcessNext();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}


struct PathRequest
{
    public Vector3 pathStart;
    public Vector3 pathEnd;
    public Action<Vector3[], bool> callback;

    /// <summary>
    /// 경로 요청 정보를 담는 구조체 인스턴스를 생성한다.
    /// nStart, nEnd, nCallback을 입력으로 받아 pathStart, pathEnd, callback 필드에 저장한다.
    /// </summary>
    public PathRequest(Vector3 nStart, Vector3 nEnd, Action<Vector3[], bool> nCallback)
    {
        pathStart = nStart;
        pathEnd = nEnd;
        callback = nCallback;
    }
}