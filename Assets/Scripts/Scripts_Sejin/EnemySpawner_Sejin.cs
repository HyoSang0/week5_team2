using UnityEngine;
using System.Collections;

public class EnemySpawner_Sejin : MonoBehaviour
{
    public GameObject enemy;
    public float spawnRate = 1.0f;

    private Coroutine spawnCoroutine;

    void Start()
    {
        spawnCoroutine = StartCoroutine(SpawnCoroutine());
    }

    IEnumerator SpawnCoroutine()
    {
        while(true)
        {
            yield return new WaitForSeconds(spawnRate);
            Instantiate(enemy, transform);
        }
    }

    public void StopSpawn()
    {
        StopCoroutine(SpawnCoroutine());
    }
}
