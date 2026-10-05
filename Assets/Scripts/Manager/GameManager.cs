using System;
using System.Collections;

using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

public class GameManager : MonoBehaviour
{
    private enum RunState { Normal, SurvivalSuccess, Infinite, Dead };
    private const int EXPERIENCE_PER_KILL = 2;
    private const int EXPERIENCE_PER_ABSORB = 10;
    // 각 항목은 해당 레벨에서 다음 레벨로 올라가는 데 필요한 경험치다.
    private static readonly int[] _levelExperienceRequirements = { 500, 700, 900, 1100, 1300, 1500, 3000 };

    public static GameManager Instance;

    [Header("Practice")]
    [SerializeField] private bool _isPracticeMode;

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
    [SerializeField] private GameObject infiniteModeButton;
    private RunState runState = RunState.Normal;

    public int score;
    const float timeLimit = 100;

    // bool isGameOver = false;

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
            // 화면 포커스를 잃어도 게임이 멈추지 않도록 백그라운드 실행을 허용한다.
            Application.runInBackground = true;
        }
    }

    void Start()
    {
        if (_isPracticeMode)
        {
            return;
        }

        infiniteModeButton.SetActive(false);
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
    /// 연습 모드가 아니면 적 결과를 통계에 기록한다. outcomeType과 enemyType을 사용하며 경험치는 볼 도착 시 별도로 지급한다.
    /// EnemyFall도 통계에는 남기지만 경험치 볼을 만들지 않는다.
    /// </summary>
    public void RecordEnemyOutcome(StatisticsManager.GameStatisticType outcomeType, EnemyPool.PoolType enemyType)
    {
        if (_isPracticeMode)
        {
            return;
        }

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
    /// 연습 모드가 아니면 경험치를 현재 레벨에 더하고 요구치를 넘으면 초과분을 다음 레벨로 넘긴다.
    /// amount를 사용하며, 여러 레벨을 건너뛰면 새 레벨마다 LevelReached를 한 번씩 발생시킨다.
    /// </summary>
    public void AddExperience(int amount)
    {
        if (_isPracticeMode || amount <= 0)
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
    /// 일반모드에서 생존 성공 상태로 전환한다. 
    /// 현재 진행 상태와 플레이어 체력을 확인하고, 성공 UI와 무한모드 버튼을 표시한 뒤 게임을 정지한다. 
    /// </summary>
    public void GameClear()
    {
        if (_isPracticeMode)
        {
            return;
        }

        if (runState != RunState.Normal)
        {
            return;
        }
        if (playerHp.playerHP <= 0)
        {
            PlayerDie();
            return;
        }
        runState = RunState.SurvivalSuccess;
        infiniteModeButton.SetActive(true);
        _gameOverGroup.SetActive(true);
        gameOverText.text = "생존 성공";
        gameOverText.color = UIPalette.Accent;
        GamePause.Pause(PauseReason.GameOver);
    }

    /// <summary>
    /// 연습 모드가 아닌 현재 플레이를 사망 상태로 종료한다. 
    /// 연습 여부와 진행 상태를 확인하며, 무한모드 버튼을 숨기고 게임 오버 화면을 표시한 뒤 일시정지한다. 
    /// </summary>
    public void PlayerDie()
    {
        if (_isPracticeMode || runState == RunState.Dead)
        {
            return;
        }
        runState = RunState.Dead;
        infiniteModeButton.SetActive(false);
        _gameOverGroup.SetActive(true);
        gameOverText.text = "게임 오버";
        gameOverText.color = UIPalette.Danger;
        GamePause.Pause(PauseReason.GameOver);
    }

    /// <summary>
    /// 생존 성공 화면에서 무한모드로 진입한다. 
    /// 현재 진행 상태와 UI 입력 잠금을 확인하고, 플레이 상태를 유지한 채 결과 화면을 닫고 종료 정지를 해제한다. 
    /// </summary>
    public void EnterInfiniteMode()
    {
        if (runState != RunState.SurvivalSuccess || _gameOverSelection.IsInputLocked)
        {
            return;
        }

        runState = RunState.Infinite;
        infiniteModeButton.SetActive(false);
        _gameOverGroup.SetActive(false);

        GamePause.Resume(PauseReason.GameOver);
    }
    // 게임 시간
    /// <summary>
    /// 일반모드의 남은 시간과 무한모드의 총 생존 시간을 갱신한다. 
    /// time을 일반모드 제한시간으로 사용하며, 제한 도달 시 생존 성공을 처리하고 사망 시 타이머를 종료한다. 
    /// </summary>
    IEnumerator StartTimer(float time)
    {
        float elapsedTime = 0f;
        while (runState != RunState.Dead)
        {
            if (runState == RunState.SurvivalSuccess || GamePause.IsPaused)
            {
                yield return null;
                continue;
            }
            elapsedTime += Time.deltaTime;
            if (runState == RunState.Normal)
            {
                float remainingTime = Mathf.Max(0, time - elapsedTime);
                timeText.text = remainingTime.ToString("F2");
                if (remainingTime <= 0)
                {
                    elapsedTime = time;
                    GameClear();
                }
            }
            else if (runState == RunState.Infinite)
            {
                timeText.text = elapsedTime.ToString("F2");
            }

            yield return null;
        }
    }

    /// <summary>
    /// 게임 오버 UI의 입력 잠금이 풀린 뒤 재시작 입력을 처리한다.
    /// _gameOverSelection.IsInputLocked가 true면 아무것도 하지 않고, 아니면 정지를 초기화한 뒤 현재 게임 씬을 다시 불러온다.
    /// </summary>
    public void RestartGame()
    {
        if (_gameOverSelection.IsInputLocked)
        {
            return;
        }

        GamePause.ResetAll();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    /// <summary>
    /// 연습 모드가 아니면 적 처치 점수를 score에 반영한다. killedEnemy의 enemyScore에 PlayerStats의 ScoreMultiplier 증강을 적용해
    /// score에 누적한 뒤 점수 텍스트를 갱신한다.
    /// PlayerStats.Instance가 없으면 증강 없이 enemyScore를 그대로 더하며, Enemy.Kill과 Enemy_NoRush.CheckHealthNr의 성공 경로에서 한 번씩만 호출된다.
    /// 통계 집계는 StatisticsManager가 담당하므로 여기서는 점수만 처리한다.
    /// </summary>
    public void AddKillScore(Enemy killedEnemy)
    {
        if (_isPracticeMode)
        {
            return;
        }

        int killScore = PlayerStats.Instance != null
            ? PlayerStats.Instance.ApplyInt(StatType.ScoreMultiplier, killedEnemy.enemyScore)
            : killedEnemy.enemyScore;

        score += killScore;
        AugmentEvents.RaiseScoreIncreased((float)killScore);
        UpdateScoreText();
    }
}
