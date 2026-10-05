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
    private Transform _visual;
    private int _experienceAmount;
    private bool _awardExperienceOnArrival;

    public DestinationMode Mode => _destinationMode;

    void Awake()
    {
        _visual = transform.GetChild(0);
    }

    /// <summary>
    /// 경험치 볼의 목적지와 크기, 도착 시 경험치 지급 여부를 설정한다.
    /// experienceAmount를 흡수 경험치와 비교해 시각 크기를 정하고, 풀 재사용 상태를 초기화한다.
    /// </summary>
    public void Initialize(
        Transform playerTarget,
        Transform uiWorldMarker,        
        int experienceAmount,
        bool awardExperienceOnArrival,
        bool forcePlayerDestination = false)
    {
        bool moveToUiMarker = !forcePlayerDestination
            && _destinationMode == DestinationMode.UiMarker
            && uiWorldMarker != null;
        target = moveToUiMarker ? uiWorldMarker : playerTarget;
        _activeOffset = moveToUiMarker ? _uiMarkerOffset : targetOffset;        
        _experienceAmount = experienceAmount;
        _awardExperienceOnArrival = awardExperienceOnArrival;
        float absorbExperience = GameManager.GetExperienceForOutcome(StatisticsManager.GameStatisticType.EnemyAbsorb);
        _visual.localScale = Vector3.one * (experienceAmount / absorbExperience);
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

        Vector3 destination = target.position + _activeOffset;
        transform.position = Vector3.MoveTowards(transform.position, destination, moveSpeed * Time.deltaTime);

        if ((transform.position - destination).sqrMagnitude > arrivalDistance * arrivalDistance)
            return;

        finished = true;
        if (_awardExperienceOnArrival)
        {
            GameManager.Instance.AddExperience(_experienceAmount);
        }        
        EffectPool.Release(gameObject);
    }
}
