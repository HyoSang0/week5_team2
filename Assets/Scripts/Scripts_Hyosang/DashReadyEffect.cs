using UnityEngine;

public class DashReadyEffect : MonoBehaviour
{
    [SerializeField] private Vector3 followOffset = new Vector3(0, 0, 0);
    [SerializeField] private float rotationSpeed = 100f;
    private Transform _player;

    void Start()
    {
        _player = GameObject.FindGameObjectWithTag("Player").transform;
    }

    void Update()
    {
        transform.position = _player.position + followOffset;
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
    }


}
