using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    [Header("Text")]
    [SerializeField] TextMeshProUGUI timeText;
    [SerializeField] TextMeshProUGUI gameOverText;
    [SerializeField] TextMeshProUGUI scoreText;

    [SerializeField] PlayerHp playerHp;

    public GameObject gameOverGroup;

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
        // gameOverText.gameObject.SetActive(false);
        gameOverGroup.SetActive(false);
        timeLimit = 60;
        StartCoroutine(StartTimer(timeLimit));
    }


    // Update is called once per frame
    void Update()
    {
        scoreText.text = string.Format("SCORE : {0}", score);
    }

    public void GameClear()
    {
        isGameOver = true;
        gameOverGroup.SetActive(true);
        // gameOverText.gameObject.SetActive(true);
        gameOverText.text = "YOU WIN";
        gameOverText.color = Color.yellow;
        Time.timeScale = 0;
    }

    public void PlayerDie()
    {
        isGameOver = true;
        gameOverGroup.SetActive(true);
        // gameOverText.gameObject.SetActive(true);
        gameOverText.text = "YOU DIE";
        gameOverText.color = Color.red;
        Time.timeScale = 0;
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

    public void RestartGame()
    {
        Time.timeScale = 1;
        SceneManager.LoadScene(0);
    }

    public void AddScore(int newScore)
    {
        score += newScore;
    }
}
