using System.Collections;
using TMPro;
using UnityEngine;


public class PlayerHp : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI hpText;
    Rigidbody rb;
    [SerializeField] GameManager gameManager;
    bool isUnBeat = false;
    bool isRushing = false;
    public int playerHP = 5;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        hpText.text = playerHP + " / 5";
    }



    // Update is called once per frame
    IEnumerator UnBeatTime()
    {
        yield return new WaitForSeconds(2);
        isUnBeat = false;
    }

    public void PlayerAttacked(int damage)
    {
        playerHP -= damage;
        gameManager.PlayerAttackedUI(playerHP);
    }

    void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.CompareTag("Enemy") || collision.gameObject.CompareTag("NoAbsortEnemy"))
        {

            if (!isUnBeat)
            { 
                isUnBeat =true;
                PlayerAttacked(1);
                if (playerHP <= 0) gameManager.PlayerDie();
                StartCoroutine(UnBeatTime());
            }
        }
    }

    public void StartRush()
    {
        isUnBeat = true;
    }

    public void EndRush()
    {
        isUnBeat = false;
    }
}
