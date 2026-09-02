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
        StartCoroutine(Spawn(PoolType.Basic, 0.5f, 0, 10));
        StartCoroutine(Spawn(PoolType.Basic, 0.1f, 5, 100));
        StartCoroutine(Spawn(PoolType.Basic, 0.1f, 10, 100));
        StartCoroutine(Spawn(PoolType.Basic, 0.1f, 10, 100));
        // StartCoroutine(Spawn(PoolType.Boom, 1f, 10, 1f));
        StartCoroutine(Spawn(PoolType.NoAbsort, 1f, 20, 1));
        StartCoroutine(Spawn(PoolType.NoRush, 0.5f, 20, 1));
        StartCoroutine(Spawn(PoolType.NoRush, 0.5f, 30, 1.5f));
        // StartCoroutine(Spawn(PoolType.Boom, 0.3f, 30, 1f));
        StartCoroutine(Spawn(PoolType.NoRush, 0.5f, 40, 2));
        StartCoroutine(Spawn(PoolType.NoAbsort, 1f, 45, 1));
        StartCoroutine(Spawn(PoolType.NoRush, 0.5f, 50, 3));
        // StartCoroutine(Spawn(PoolType.Boom, 0.1f, 57, 1f));



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
    }
    void SpawnBoom()
    {
        // 좌표 지정 및 바로 터뜨리기
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
            float value = (Random.Range(0, 2) == 0) ? -50f : 50f;
            rad = (deg + value) * Mathf.Deg2Rad;
            spawnPos = new Vector3(Mathf.Cos(rad) * dis, 0, Mathf.Sin(rad) * dis);
        }

        return new Vector3(Mathf.Cos(rad) * dis, 0, Mathf.Sin(rad) * dis);
    }


}
