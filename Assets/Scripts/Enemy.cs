using UnityEngine;

public class Enemy : MonoBehaviour
{
    public int health;
    public int speed = 5;
    public PlayerController player;

    private Rigidbody enemyRb;
    void Awake()
    {
        enemyRb = GetComponent<Rigidbody>();
        player = FindObjectOfType<PlayerController>();
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
        Vector3 moveDirection = player.transform.position - transform.position;
        enemyRb.linearVelocity = moveDirection.normalized * speed;
    }
}
