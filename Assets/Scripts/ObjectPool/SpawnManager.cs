using System.Collections;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

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
        StartCoroutine(Spawn(0));
        if (secondSpawnerActive) StartCoroutine(Spawn(15));
        if (thirdSpawnerActive) StartCoroutine(Spawn(25));

    }

    void Update()
    {

    }
    IEnumerator Spawn(int delayTime)
    {
        yield return new WaitForSeconds(delayTime);
        float dia = worldDia / 2;
        while (!gameClear)
        {
            int rand = Random.Range(0, 360);
            enemyPool.SpawnEnemy(AngleToVector(rand, dia-1), new Quaternion(0, 0, 0, 0));
            yield return new WaitForSeconds(spawnRate);
            if (spawnRate > 0.2) spawnRate -= 0.1f;
        }
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
