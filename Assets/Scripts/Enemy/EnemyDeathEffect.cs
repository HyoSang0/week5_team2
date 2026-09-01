using UnityEngine;

public class EnemyDeathEffect : MonoBehaviour
{
    float timer = 0f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        timer += Time.deltaTime;
        if(timer >= 1f)
        {
            Destroy(gameObject);
        }
    }
}
