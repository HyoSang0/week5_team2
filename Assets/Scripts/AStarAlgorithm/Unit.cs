using System.Collections;
using UnityEngine;

public class Unit : MonoBehaviour
{
    public Transform target;
    float speed = 5f;
    Vector3[] path;
    int targetIndex;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        PathRequestManager.RequestPath(transform.position, target.position, OnPathFound);    
    }

    public void OnPathFound(Vector3[] newPath, bool pathSuccessful)
    {
        if (pathSuccessful)
        {
            path = newPath;
            StopCoroutine("FollowPath");
            StartCoroutine("FollowPath");
        }
    }

    IEnumerator FollowPath()
    {
        Vector3 currentWaypoint = path[0];

        while (true)
        {
            if (transform.position == currentWaypoint)
            {
                targetIndex++;
                if (targetIndex >= path.Length)
                {
                    yield break;
                }
                currentWaypoint = path[targetIndex];
            }

            transform.position = Vector3.MoveTowards(transform.position, currentWaypoint, speed * Time.deltaTime);
            yield return null;
        }
    }
}
