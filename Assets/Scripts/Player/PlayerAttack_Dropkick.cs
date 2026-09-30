using System;
using System.Collections;
using UnityEngine;

[Obsolete]
public class PlayerAttack_Dropkick : MonoBehaviour
{
    private InputSystem_Actions inputActions;
    private RaycastHit hit;
    public BoxCollider dropkickRangeRb;
    public GameObject kickObject;
    public float kickSeconds = 0.25f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        inputActions = new InputSystem_Actions();
        dropkickRangeRb = GameObject.Find("Dropkick_Range").GetComponent<BoxCollider>();
    }

    void Start()
    {
        kickObject.SetActive(false);
    }

    void Update()
    {
        //inputActions.Player.Attack.performed += ctx => Dropkick();
    }

    void OnEnable()
    {
        inputActions.Enable();
    }

    void OnDisable()
    {
        inputActions.Disable();
    }

    public void Dropkick()
    {
        StartCoroutine(DropkickSequence());
    }

    IEnumerator DropkickSequence()
    {
        //박스 콜라이더와 킥 오브젝트 활성화
        dropkickRangeRb.enabled = true;
        kickObject.SetActive(true);

        //킥 오브젝트를 kickSeconds 동안 앞으로 부드럽게 이동시킴
        float timeElapsed = 0f;
        while (timeElapsed < kickSeconds)
        {
            timeElapsed += Time.deltaTime;
            kickObject.transform.localPosition = new Vector3(0f, 0f, Mathf.Lerp(0f, 1f, timeElapsed / kickSeconds));
            yield return null;
        }
        yield return new WaitForSeconds(0.25f);
        //킥 오브젝트 원위치 및 박스 콜라이더, 킥 오브젝트 비활성화
        kickObject.transform.localPosition = new Vector3(0f, 0.5f, 0f);
        kickObject.SetActive(false);
        dropkickRangeRb.enabled = false;
    }
}
