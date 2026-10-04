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
    private const int EXPERIENCE_TO_LEVEL_2 = 1000;
    private const int EXPERIENCE_TO_LEVEL_3 = 5000;
    private const int EXPERIENCE_TO_LEVEL_4 = 10000;
    private const int EXPERIENCE_TO_LEVEL_5 = 15000;
    private const int EXPERIENCE_STEP_AFTER_LEVEL_4 = 5000;

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
        }
    }

    void Start()
    {
        UpdateScoreText();
        StartCoroutine(StartTimer(timeLimit));
    }

    /// <summary>
    /// 현재 런의 레벨을 반환한다. _level을 사용하며 1부터 시작해 누적 경험치가 다음 레벨 기준값에 도달할 때마다 1씩 오른다.
    /// </summary>
    public int Level => _level;

    /// <summary>
    /// 현재 런에 누적된 총 경험치를 반환한다. StatisticsManager의 처치·흡수 누적 수에 각 단위 경험치를 곱해 합산하며 낙사는 경험치가 없다.
    /// </summary>
    public int TotalExperience =>
        StatisticsManager.Instance.GetCount(StatisticsManager.GameStatisticType.EnemyKill) * EXPERIENCE_PER_KILL
        + StatisticsManager.Instance.GetCount(StatisticsManager.GameStatisticType.EnemyAbsorb) * EXPERIENCE_PER_ABSORB;

    /// <summary>
    /// 적 결과를 통계에 기록하고 누적 경험치가 다음 레벨 기준값을 넘으면 레벨을 갱신한다.
    /// outcomeType과 enemyType을 StatisticsManager.Instance.Record에 정확히 한 번 전달하며, EnemyFall은 경험치가 없어 레벨이 변하지 않는다.
    /// 한 번의 기록으로 여러 레벨을 넘으면 새로 도달한 각 레벨마다 LevelReached를 한 번씩 발생시키고 _level을 갱신한다.
    /// </summary>
    public void RecordEnemyOutcome(StatisticsManager.GameStatisticType outcomeType, EnemyPool.PoolType enemyType)
    {
        StatisticsManager.Instance.Record(outcomeType, enemyType);

        // 누적 수는 줄지 않으므로 레벨도 단조 증가한다. 건너뛴 레벨을 드롭하지 않고 순서대로 발생시킨다.
        while (TotalExperience >= GetNextLevelRequiredExperience(_level))
        {
            _level++;
            LevelReached?.Invoke(_level);
        }
    }

    /// <summary>
    /// 현재 레벨에서 다음 레벨로 오르기 위해 필요한 누적 총 경험치 기준값을 반환한다.
    /// currentLevel(1~4)에는 고정 기준값을 사용하며, 5 이상부터는 레벨마다 EXPERIENCE_STEP_AFTER_LEVEL_4씩 증가한 값을 반환한다.
    /// </summary>
    private static int GetNextLevelRequiredExperience(int currentLevel)
    {
        switch (currentLevel)
        {
            case 1:
                return EXPERIENCE_TO_LEVEL_2;
            case 2:
                return EXPERIENCE_TO_LEVEL_3;
            case 3:
                return EXPERIENCE_TO_LEVEL_4;
            case 4:
                return EXPERIENCE_TO_LEVEL_5;
            default:
                // 4 -> 5 기준값(15000)에서 한 레벨당 5000씩 누적된다.
                return EXPERIENCE_TO_LEVEL_5 + (currentLevel - 4) * EXPERIENCE_STEP_AFTER_LEVEL_4;
        }
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
