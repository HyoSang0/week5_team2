using UnityEngine;
using System.Collections;

public class PlayerAttack_Dropkick : MonoBehaviour
{
    private InputSystem_Actions inputActions;
    private RaycastHit hit;

    public BoxCollider dropkickRangeRb;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        inputActions = new InputSystem_Actions();
        dropkickRangeRb = GameObject.Find("Dropkick_Range").GetComponent<BoxCollider>();
    }

    // Update is called once per frame
    void Update()
    {
        inputActions.Player.Attack.performed += ctx => Dropkick();
    }

    void OnEnable()
    {
        inputActions.Enable();
    }

    void OnDisable()
    {
        inputActions.Disable();
    }

    void Dropkick()
    {
        StartCoroutine(DropkickSequence());
    }

    IEnumerator DropkickSequence()
    {
        dropkickRangeRb.enabled = true;
        yield return new WaitForSeconds(0.5f);
        dropkickRangeRb.enabled = false;
    }
}
