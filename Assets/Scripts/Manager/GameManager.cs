using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.InputSystem.LowLevel.InputStateHistory;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    [Header("Text")]
    [SerializeField] TextMeshProUGUI timeText;
    [SerializeField] TextMeshProUGUI hpText;
    [SerializeField] TextMeshProUGUI gameOverText;    

    [SerializeField] PlayerHp playerHp;
    float timeLimit;

    bool isGameOver = false;
    public bool isUnBeat = false;

    void Awake()
    {
        
        Instance = this;
        if(Instance != null && Instance != this)
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
        gameOverText.gameObject.SetActive(false);
        hpText.text = "5 / 5";
        timeLimit = 60;
        StartCoroutine(StartTimer(timeLimit));
    }


    // Update is called once per frame
    void Update()
    {
        
    }

    public void GameClear()
    {
        isGameOver = true;
        gameOverText.gameObject.SetActive(true);
        gameOverText.text = "YOU WIN";
        gameOverText.color = Color.yellow;
        Time.timeScale = 0;
    }

    public void PlayerDie()
    {
        isGameOver = true;
        gameOverText.gameObject.SetActive(true);
        gameOverText.text = "YOU DIE";
        gameOverText.color = Color.red;
        Time.timeScale = 0;
    }

    // UI는 한 곳에서 관리하는 것이 좋음
    public void PlayerAttackedUI(int hp)
    {
        hpText.text = hp + " / 5";
    }

    // 게임 시간
    IEnumerator StartTimer(float time)
    {
        float curTime = time;
        while (curTime > 0)
        {
            curTime -= Time.deltaTime;
            // 소수점 2자리 까지 표현
            timeText.text = string.Format("{0:0.##}", curTime);
            yield return null;

            if (curTime <= 0)
            {
                curTime = 0;
                GameClear();
                yield break;
            }
        }
    }
}
