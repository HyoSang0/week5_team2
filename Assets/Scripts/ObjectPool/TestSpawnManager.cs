using Unity.VisualScripting;
using UnityEngine;

public class TestSpawnManager : MonoBehaviour
{
    [SerializeField] EnemyPool enemyPool;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        InvokeRepeating("testSpawn", 1, 1);
    }

    void testSpawn()
    {
        enemyPool.SpawnEnemy(new Vector3 (5, 1, 0), new Quaternion(0,0,0,0));
    }

}
