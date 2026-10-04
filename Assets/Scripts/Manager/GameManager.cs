using System.Collections;
using System.Collections.Generic;

using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

public class GameManager : MonoBehaviour
{
    /// <summary>적 결과의 유형 분류. 낙하 사망은 Fall, 흡수 성공은 Absorb, 전투 사망은 Kill이다.</summary>
    public enum EnemyOutcomeType
    {
        Fall,
        Absorb,
        Kill,
    }

    public static GameManager Instance;
    [Header("Text")]
    [SerializeField] TextMeshProUGUI timeText;
    [SerializeField] TextMeshProUGUI gameOverText;
    [SerializeField] TextMeshProUGUI scoreText;

    [SerializeField] PlayerHp playerHp;

    [Header("Game Over")]
    [FormerlySerializedAs("gameOverGroup")]
    [SerializeField] private GameObject _gameOverGroup;
    [SerializeField] private UIDefaultSelection _gameOverSelection;

    [Header("Enemy Outcome")]
    private readonly Dictionary<EnemyOutcomeType, int> _outcomeCounts = new Dictionary<EnemyOutcomeType, int>();

    public int score;
    const float timeLimit = 100;

    bool isGameOver = false;
    public bool isUnBeat = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    void Start()
    {
        UpdateScoreText();
        StartCoroutine(StartTimer(timeLimit));
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
    /// 지정한 유형의 적 결과 누적 수를 반환한다. outcomeType으로 _outcomeCounts를 조회하며, 아직 기록되지 않은 유형은 0을 반환한다.
    /// </summary>
    public int GetOutcomeCount(EnemyOutcomeType outcomeType)
    {
        return _outcomeCounts.TryGetValue(outcomeType, out int count) ? count : 0;
    }

    /// <summary>
    /// 적 낙하 결과를 집계한다. Fall 유형의 누적 수를 1 증가시키며, 점수 계산에는 관여하지 않는다.
    /// </summary>
    public void RecordEnemyFall()
    {
        IncrementOutcomeCount(EnemyOutcomeType.Fall);
    }

    /// <summary>
    /// 적 흡수 성공 결과를 집계한다. Absorb 유형의 누적 수를 1 증가시키며, 점수 계산에는 관여하지 않는다.
    /// </summary>
    public void RecordEnemyAbsorb()
    {
        IncrementOutcomeCount(EnemyOutcomeType.Absorb);
    }

    /// <summary>
    /// 적 전투 사망 결과를 집계하고 킬 점수를 반영한다. Kill 유형의 누적 수를 1 증가시키고,
    /// killedEnemy의 enemyScore에 PlayerStats의 ScoreMultiplier 증강을 적용해 score에 누적한 뒤 점수 텍스트를 갱신한다.
    /// PlayerStats.Instance가 없으면 증강 없이 enemyScore를 그대로 더하며, Enemy.Kill과 Enemy_NoRush.CheckHealthNr의 성공 경로에서 한 번씩만 호출된다.
    /// </summary>
    public void RecordEnemyKill(Enemy killedEnemy)
    {
        IncrementOutcomeCount(EnemyOutcomeType.Kill);

        int killScore = PlayerStats.Instance != null
            ? PlayerStats.Instance.ApplyInt(StatType.ScoreMultiplier, killedEnemy.enemyScore)
            : killedEnemy.enemyScore;

        score += killScore;
        UpdateScoreText();
    }

    /// <summary>
    /// 적 결과 유형의 누적 수를 1 증가시킨다. outcomeType으로 _outcomeCounts를 갱신하며, 첫 기록인 유형은 0에서 시작한다.
    /// </summary>
    private void IncrementOutcomeCount(EnemyOutcomeType outcomeType)
    {
        _outcomeCounts[outcomeType] = GetOutcomeCount(outcomeType) + 1;
    }
}
