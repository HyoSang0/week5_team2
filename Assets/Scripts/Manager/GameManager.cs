using System;
using System.Collections;

using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

public class GameManager : MonoBehaviour
{
    private const int EXPERIENCE_PER_KILL = 2;
    private const int EXPERIENCE_PER_ABSORB = 10;
    // 각 항목은 해당 레벨에서 다음 레벨로 올라가는 데 필요한 경험치다.
    private static readonly int[] _levelExperienceRequirements = { 1000, 4000, 5000, 5000 };

    public static GameManager Instance;

    /// <summary>새 레벨에 도달할 때마다 도달한 레벨 값을 인자로 발생하는 이벤트다.</summary>
    public event Action<int> LevelReached;

    [Header("Text")]
    [SerializeField] TextMeshProUGUI timeText;
    [SerializeField] TextMeshProUGUI gameOverText;
    [SerializeField] TextMeshProUGUI scoreText;

    [SerializeField] PlayerHp playerHp;

    [Header("Game Over")]
    [FormerlySerializedAs("gameOverGroup")]
    [SerializeField] private GameObject _gameOverGroup;
    [SerializeField] private UIDefaultSelection _gameOverSelection;

    public int score;
    const float timeLimit = 100;

    bool isGameOver = false;
    public bool isUnBeat = false;

    [Header("Level")]
    private int _level = 1;
    private int _currentLevelExperience;
    private int _nextLevelRequirement;
    public event Action ExperienceChanged;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            // 중복 인스턴스가 파괴되는 경로가 아닌 실제 런 시작에서만 런 단위 통계를 초기화한다.
            StatisticsManager.Instance.ResetRun();
            _level = 1;
            _currentLevelExperience = 0;
            _nextLevelRequirement = GetRequirementForLevel(_level);
        }
    }

    void Start()
    {
        UpdateScoreText();
        StartCoroutine(StartTimer(timeLimit));
    }

    /// <summary>
    /// 현재 런의 레벨을 반환한다. _level은 1부터 시작하며 경험치 요구치를 채울 때마다 1씩 오른다.
    /// </summary>
    public int Level => _level;

    /// <summary>
    /// 현재 레벨에서 쌓인 경험치를 반환한다. 레벨업 후 남은 초과분은 다음 레벨에 이월된다.
    /// </summary>
    public int CurrentLevelExperience => _currentLevelExperience;

    /// <summary>
    /// 다음 레벨까지 필요한 경험치 총량을 반환한다. 현재 _level에 대응하는 목록 값을 사용한다.
    /// </summary>
    public int NextLevelRequirement => _nextLevelRequirement;

    /// <summary>
    /// 적 결과를 통계에 기록한다. outcomeType과 enemyType을 사용하며 경험치는 볼 도착 시 별도로 지급한다.
    /// EnemyFall도 통계에는 남기지만 경험치 볼을 만들지 않는다.
    /// </summary>
    public void RecordEnemyOutcome(StatisticsManager.GameStatisticType outcomeType, EnemyPool.PoolType enemyType)
    {
        StatisticsManager.Instance.Record(outcomeType, enemyType);
    }

    /// <summary>
    /// 적 결과 유형에 해당하는 경험치 보상을 반환한다. 전투 처치는 2, 흡수는 10, 낙사는 0이다.
    /// </summary>
    public static int GetExperienceForOutcome(StatisticsManager.GameStatisticType outcomeType)
    {
        switch (outcomeType)
        {
            case StatisticsManager.GameStatisticType.EnemyKill:
                return EXPERIENCE_PER_KILL;
            case StatisticsManager.GameStatisticType.EnemyAbsorb:
                return EXPERIENCE_PER_ABSORB;
            default:
                return 0;
        }
    }

    /// <summary>
    /// 경험치를 현재 레벨에 더하고 요구치를 넘으면 초과분을 다음 레벨로 넘긴다.
    /// amount를 사용하며, 여러 레벨을 건너뛰면 새 레벨마다 LevelReached를 한 번씩 발생시킨다.
    /// </summary>
    public void AddExperience(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        _currentLevelExperience += amount;
        while (_currentLevelExperience >= _nextLevelRequirement)
        {
            _currentLevelExperience -= _nextLevelRequirement;
            _level++;
            _nextLevelRequirement = GetRequirementForLevel(_level);
            LevelReached?.Invoke(_level);
        }

        ExperienceChanged?.Invoke();
    }

    /// <summary>
    /// currentLevel에서 다음 레벨로 올라가는 데 필요한 경험치를 목록에서 반환한다.
    /// 목록을 모두 사용한 뒤에는 마지막 요구치를 반복한다.
    /// </summary>
    private static int GetRequirementForLevel(int currentLevel)
    {
        int index = Mathf.Min(currentLevel - 1, _levelExperienceRequirements.Length - 1);
        return _levelExperienceRequirements[index];
    }

    /// <summary>
    /// 현재 score를 "점수: {0}" 형식의 텍스트로 scoreText에 반영한다.
    /// </summary>
    private void UpdateScoreText()
    {
        scoreText.text = string.Format("점수: {0}", score);
    }

    /// <summary>
    /// 게임 클리어 처리를 한다.
    /// 게임 오버 그룹을 표시하고 "생존 성공" 문구를 UIPalette.Accent 색으로 표시한 뒤 GamePause로 일시정지한다.
    /// </summary>
    public void GameClear()
    {
        isGameOver = true;
        _gameOverGroup.SetActive(true);
        gameOverText.text = "생존 성공";
        gameOverText.color = UIPalette.Accent;
        GamePause.Pause(PauseReason.GameOver);
    }

    /// <summary>
    /// 플레이어 사망 처리를 한다.
    /// 게임 오버 그룹을 표시하고 "게임 오버" 문구를 UIPalette.Danger 색으로 표시한 뒤 GamePause로 일시정지한다.
    /// </summary>
    public void PlayerDie()
    {
        isGameOver = true;
        _gameOverGroup.SetActive(true);
        gameOverText.text = "게임 오버";
        gameOverText.color = UIPalette.Danger;
        GamePause.Pause(PauseReason.GameOver);
    }

    // 게임 시간
    IEnumerator StartTimer(float time)
    {
        float curTime = time;
        while (curTime > 0)
        {
            curTime -= Time.deltaTime;
            // 0 미만으로 내려가지 않게 방지
            curTime = Mathf.Max(0, curTime);
            // 소수점 2자리 까지 표현
            timeText.text = curTime.ToString("F2");
            yield return null;
        }
        GameClear();
    }

    /// <summary>
    /// 게임 오버 UI의 입력 잠금이 풀린 뒤 재시작 입력을 처리한다.
    /// _gameOverSelection.IsInputLocked가 true면 아무것도 하지 않고, 아니면 GamePause.ResetAll로 일시정지를 초기화한 뒤 씬을 다시 불러온다.
    /// </summary>
    public void RestartGame()
    {
        if (_gameOverSelection.IsInputLocked)
        {
            return;
        }

        GamePause.ResetAll();
        SceneManager.LoadScene(0);
    }

    /// <summary>
    /// 적 처치 점수를 score에 반영한다. killedEnemy의 enemyScore에 PlayerStats의 ScoreMultiplier 증강을 적용해
    /// score에 누적한 뒤 점수 텍스트를 갱신한다.
    /// PlayerStats.Instance가 없으면 증강 없이 enemyScore를 그대로 더하며, Enemy.Kill과 Enemy_NoRush.CheckHealthNr의 성공 경로에서 한 번씩만 호출된다.
    /// 통계 집계는 StatisticsManager가 담당하므로 여기서는 점수만 처리한다.
    /// </summary>
    public void AddKillScore(Enemy killedEnemy)
    {
        int killScore = PlayerStats.Instance != null
            ? PlayerStats.Instance.ApplyInt(StatType.ScoreMultiplier, killedEnemy.enemyScore)
            : killedEnemy.enemyScore;

        score += killScore;
        UpdateScoreText();
    }
}
