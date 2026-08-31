using UnityEngine;

public class Enemy : MonoBehaviour
{
    public int health;
    public int speed = 5;
    //public Player player;

    private Rigidbody enemyRb;
    void Awake()
    {
        enemyRb = GetComponent<Rigidbody>();
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        MoveTowardsPlayer();
    }

    void MoveTowardsPlayer()
    {
        // Vector3 moveDirection = player.transform.position - transform.position;
        // enemyRb.velocity = moveDirection.normalized * speed;
    }
}
