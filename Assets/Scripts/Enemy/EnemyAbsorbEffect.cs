using UnityEngine;

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
    private bool finished;
    private Vector3 _activeOffset;

    public DestinationMode Mode => _destinationMode;

    /// <summary>
    /// 빛 구슬이 날아갈 대상과 함께 풀 재사용 상태를 초기화한다.
    /// 목적지 모드에 따라 playerTarget 또는 uiWorldMarker를 저장하고 finished 플래그를 되돌린다.
    /// </summary>
    public void Initialize(Transform playerTarget, Transform uiWorldMarker)
    {
        bool moveToUiMarker = _destinationMode == DestinationMode.UiMarker;
        target = moveToUiMarker ? uiWorldMarker : playerTarget;
        _activeOffset = moveToUiMarker ? _uiMarkerOffset : targetOffset;
        // 풀 재사용 시 이전 도착 여부가 남지 않도록 되돌린다.
        finished = false;
    }

    private void Update()
    {
        if (finished)
            return;

        if (target == null)
        {
            finished = true;
            EffectPool.Release(gameObject);
            return;
        }

        Vector3 destination = target.position + targetOffset + _activeOffset;
        transform.position = Vector3.MoveTowards(transform.position, destination, moveSpeed * Time.deltaTime);

        if ((transform.position - destination).sqrMagnitude > arrivalDistance * arrivalDistance)
            return;

        finished = true;
        EffectPool.Release(gameObject);
    }
}