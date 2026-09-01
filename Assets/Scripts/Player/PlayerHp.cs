using System.Collections;
using TMPro;
using UnityEngine;


public class PlayerHp : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI hpText;
    Rigidbody rb;
    [SerializeField] GameManager gameManager;
    bool isUnBeat = false;
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

    void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.CompareTag("Enemy"))
        {

            if (!isUnBeat)
            { 
                isUnBeat =true;
                gameManager.PlayerAttacked(--playerHP);
                if (playerHP == 0) gameManager.PlayerDie();
                StartCoroutine(UnBeatTime());
            }
        }
    }
}
