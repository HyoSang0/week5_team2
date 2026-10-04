using System;
using System.Collections;

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
    private int _enemyFallCount;
    private int _enemyAbsorbCount;
    private int _enemyKillCount;
    private int _killRewardBaseSubtotal;
    private int _killScoreSubtotal;

    /// <summary>
    /// 적 결과가 확정될 때 발생하는 이벤트. 유형(Fall 낙하 사망, Absorb 흡수 성공, Kill 전투 사망)과 대상 Enemy를 전달한다.
    /// </summary>
    public event Action<EnemyOutcomeType, Enemy> EnemyOutcome;

    /// <summary>이번 런에서 낙하로 사망한 적의 누적 수. GameManager 인스턴스 수명 동안 유지된다.</summary>
    public int EnemyFallCount => _enemyFallCount;

    /// <summary>이번 런에서 성공적으로 흡수된 적의 누적 수. GameManager 인스턴스 수명 동안 유지된다.</summary>
    public int EnemyAbsorbCount => _enemyAbsorbCount;

    /// <summary>이번 런에서 전투로 처치한 적의 누적 수. GameManager 인스턴스 수명 동안 유지된다.</summary>
    public int EnemyKillCount => _enemyKillCount;

    /// <summary>이번 런에서 처치된 적의 enemyScore를 증강 없이 합산한 기준 소계. AddScore와 별개이며 표시 점수에는 영향을 주지 않는다.</summary>
    public int KillRewardBaseSubtotal => _killRewardBaseSubtotal;

    /// <summary>이번 런에서 킬마다 enemyScore에 당시 ScoreMultiplier를 적용해 누적한 킬 점수 소계. 표시 점수의 유일한 원천이다.</summary>
    public int KillScoreSubtotal => _killScoreSubtotal;

    public int score;
    float timeLimit;

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
        timeLimit = 60;
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
    /// 적 낙하 결과를 집계한다. fallEnemy 대상으로 _enemyFallCount를 1 증가시키고 Fall 유형의 EnemyOutcome 이벤트를 발생시킨다.
    /// 점수 계산에는 관여하지 않는다.
    /// </summary>
    public void RecordEnemyFall(Enemy fallEnemy)
    {
        _enemyFallCount++;
        EnemyOutcome?.Invoke(EnemyOutcomeType.Fall, fallEnemy);
    }

    /// <summary>
    /// 적 흡수 성공 결과를 집계한다. absorbedEnemy 대상으로 _enemyAbsorbCount를 1 증가시키고 Absorb 유형의 EnemyOutcome 이벤트를 발생시킨다.
    /// 점수 계산에는 관여하지 않는다.
    /// </summary>
    public void RecordEnemyAbsorb(Enemy absorbedEnemy)
    {
        _enemyAbsorbCount++;
        EnemyOutcome?.Invoke(EnemyOutcomeType.Absorb, absorbedEnemy);
    }

    /// <summary>
    /// 적 전투 사망 결과를 집계한다. killedEnemy 대상으로 _enemyKillCount를 1 증가시키고 enemyScore를 증강 없이 _killRewardBaseSubtotal에 더한 뒤 Kill 유형의 EnemyOutcome 이벤트를 발생시킨다.
    /// 표시 점수에는 관여하지 않으며, Enemy.Kill과 Enemy_NoRush.CheckHealthNr의 성공 경로에서 한 번씩만 호출된다.
    /// </summary>
    public void RecordEnemyKill(Enemy killedEnemy)
    {
        _enemyKillCount++;
        _killRewardBaseSubtotal += killedEnemy.enemyScore;
        EnemyOutcome?.Invoke(EnemyOutcomeType.Kill, killedEnemy);
    }

    /// <summary>
    /// 킬 보상을 등록한다. newScore에 PlayerStats의 ScoreMultiplier 증강을 적용한 값을 킬 점수 소계(_killScoreSubtotal)에 누적하고,
    /// score를 그 소계로 설정한 뒤 점수 텍스트를 갱신한다. PlayerStats.Instance가 없으면 newScore를 그대로 누적한다.
    /// 총합에 multiplier를 재적용하지 않도록 매 킬 시점의 증강 값을 소계에 더한다.
    /// </summary>
    public void AddScore(int newScore)
    {
        if (PlayerStats.Instance != null)
        {
            _killScoreSubtotal += PlayerStats.Instance.ApplyInt(StatType.ScoreMultiplier, newScore);
        }
        else
        {
            _killScoreSubtotal += newScore;
        }

        score = _killScoreSubtotal;
        UpdateScoreText();
    }
}
