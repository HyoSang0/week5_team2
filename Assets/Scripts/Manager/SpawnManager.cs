using System.Collections;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using static EnemyPool;

public class SpawnManager : MonoBehaviour
{
    EnemyPool enemyPool;
    GameManager gameManager;
    bool gameClear = false;
    [SerializeField] float spawnRate = 1f;
    [SerializeField] float worldDia;
    [SerializeField] GameObject player;
    // 테스트 단계에서 난이도 조절 용이를 위해 SerializeField 적용
    [SerializeField] bool secondSpawnerActive = true;
    [SerializeField] bool thirdSpawnerActive = true;
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    private void Awake()
    {
        player = GameObject.Find("Player");
        enemyPool = GetComponent<EnemyPool>();
        gameManager = GetComponent<GameManager>();
    }

    void Start()
    {
        // 게임 난이도 디자인
        StartCoroutine(Spawn(PoolType.Basic, 0.3f, 0, 100));
        StartCoroutine(Spawn(PoolType.NoAbsort, 1, 20, 3));
        StartCoroutine(Spawn(PoolType.NoRush, 0.5f, 7, 1));
    }

    /// <summary>
    /// 적을 소환하는 코루틴
    /// </summary>
    /// <param name="poolType">적의 enum 타입</param>
    /// <param name="spawnRate">소환 주기</param>
    /// <param name="delay">시작 지연 시간</param>
    /// <param name="continueTime">지속 시간</param>
    /// <returns></returns>
    IEnumerator Spawn(PoolType poolType, float spawnRate, float delay, float continueTime)
    {
        yield return new WaitForSeconds(delay);

        float dia = worldDia / 2;

        float time = continueTime;


        while (!gameClear && time > 0)
        {
            int rand = Random.Range(0, 360);
            enemyPool.SpawnEnemy(AngleToVector(rand, dia - 1), poolType);
            yield return new WaitForSeconds(spawnRate);
            time -= spawnRate;
        }
                



        /*
        yield return new WaitForSeconds(delayTime);
        float dia = worldDia / 2;

        while (!gameClear)
        {
            int rand = Random.Range(0, 360);
            enemyPool.SpawnEnemy(AngleToVector(rand, dia-1), EnemyPool.PoolType.Basic);
            yield return new WaitForSeconds(spawnRate);
            if (spawnRate > 0.2) spawnRate -= 0.1f;
        }*/
    }


    // 적 스폰 위치 계산용
    Vector3 AngleToVector(float deg, float dis)
    {
        var rad = deg * Mathf.Deg2Rad;
        Vector3 spawnPos = new Vector3(Mathf.Cos(rad) * dis, 0, Mathf.Sin(rad) * dis);

        // 플레이어랑 너무 가까운 좌표를 뽑으면 위치를 바꿈.
        if(Mathf.Abs(player.transform.position.x - spawnPos.x) < 5 && Mathf.Abs(player.transform.position.z - spawnPos.z) < 5)
        {
            // 0 이면 음수(시계 반대 방향), 1이면 양수 방향. 
            float value = (Random.Range(0, 2) == 0) ? -40f : 40f;
            rad = (deg + value) * Mathf.Deg2Rad;
            spawnPos = new Vector3(Mathf.Cos(rad) * dis, 0, Mathf.Sin(rad) * dis);
        }

        return new Vector3(Mathf.Cos(rad) * dis, 0, Mathf.Sin(rad) * dis);
    }


}
