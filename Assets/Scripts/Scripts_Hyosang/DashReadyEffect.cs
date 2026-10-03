using UnityEngine;

public class DashReadyEffect : MonoBehaviour
{
    [SerializeField] private Vector3 followOffset = new Vector3(0, 0, 0);
    [SerializeField] private float rotationSpeed = 100f;
    private Transform player;

    void OnEnable()
    {
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player").transform;
        }

        transform.position = player.position + followOffset;
    }

    void Update()
    {
        transform.position = player.position + followOffset;
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
    }


}
