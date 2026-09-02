using UnityEngine;

public class bing_gle : MonoBehaviour
{

    void Update()
    {
        transform.Rotate(new Vector3(0, 1, 0) * 50f * Time.deltaTime, Space.World);
    }
}
