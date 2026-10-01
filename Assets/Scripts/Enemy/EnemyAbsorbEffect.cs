using UnityEngine;
using UnityEngine.Events;

public enum DestinationMode
{
    Player,
    UiMarker
}

public class EnemyAbsorbEffect : MonoBehaviour
{
    [SerializeField, Min(0.01f)] private float moveSpeed = 16f;
    [SerializeField, Min(0.01f)] private float arrivalDistance = 0.15f;
    [SerializeField] private Vector3 targetOffset = Vector3.up;
    [SerializeField] private DestinationMode _destinationMode = DestinationMode.Player;
    [SerializeField] private Vector3 _uiMarkerOffset = Vector3.zero;
    private Transform target;
    private UnityEvent rewardOnArrival;
    private bool finished;
    private Vector3 _activeOffset;

    public DestinationMode Mode => _destinationMode;

    public void Initialize(Transform playerTarget, Transform uiWorldMarker, UnityEvent reward)
    {
        bool moveToUiMarker = _destinationMode == DestinationMode.UiMarker;
        target = moveToUiMarker ? uiWorldMarker : playerTarget;
        _activeOffset = moveToUiMarker ? _uiMarkerOffset : targetOffset;
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

        Vector3 destination = target.position + targetOffset + _activeOffset;
        transform.position = Vector3.MoveTowards(transform.position, destination, moveSpeed * Time.deltaTime);

        if ((transform.position - destination).sqrMagnitude > arrivalDistance * arrivalDistance)
            return;

        finished = true;
        rewardOnArrival?.Invoke();
        Destroy(gameObject);
    }
}