using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

public class GameManager : MonoBehaviour
{
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
    /// newScore에 PlayerStats의 ScoreMultiplier 증강을 적용한 값을 score에 더하고 점수 텍스트를 갱신한다.
    /// PlayerStats.Instance가 없으면 newScore를 그대로 더한다.
    /// </summary>
    public void AddScore(int newScore)
    {
        if (PlayerStats.Instance != null)
        {
            score += PlayerStats.Instance.ApplyInt(StatType.ScoreMultiplier, newScore);
        }
        else
        {
            score += newScore;
        }

        UpdateScoreText();
    }
}
