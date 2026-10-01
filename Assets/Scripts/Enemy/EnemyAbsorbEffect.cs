using UnityEngine;
using UnityEngine.Events;

public class EnemyAbsorbEffect : MonoBehaviour
{
    [SerializeField, Min(0.01f)] private float moveSpeed = 16f;
    [SerializeField, Min(0.01f)] private float arrivalDistance = 0.15f;
    [SerializeField] private Vector3 targetOffset = Vector3.up;

    private Transform target;
    private UnityEvent rewardOnArrival;
    private bool finished;

    public void Initialize(Transform playerTarget, UnityEvent reward)
    {
        target = playerTarget;
        rewardOnArrival = reward;
    }

    private void Update()
    {
        if (finished)
            return;

        if (target == null)
        {
            finished = true;
            Destroy(gameObject);
            return;
        }

        Vector3 destination = target.position + targetOffset;
        transform.position = Vector3.MoveTowards(transform.position, destination, moveSpeed * Time.deltaTime);

        if ((transform.position - destination).sqrMagnitude > arrivalDistance * arrivalDistance)
            return;

        finished = true;
        rewardOnArrival?.Invoke();
        Destroy(gameObject);
    }
}