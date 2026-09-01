using UnityEngine;
using System.Collections;

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
        dropkickRangeRb.enabled = true;
        kickObject.SetActive(true);
        float timeElapsed = 0f;
        while (timeElapsed < kickSeconds)
        {
            timeElapsed += Time.deltaTime;
            kickObject.transform.localPosition = new Vector3(0f, 0f, Mathf.Lerp(0f, 1f, timeElapsed / kickSeconds));
            yield return null;
        }
        yield return new WaitForSeconds(0.25f);
        kickObject.transform.localPosition = new Vector3(0f, 0.5f, 0f);
        kickObject.SetActive(false);
        dropkickRangeRb.enabled = false;
    }
}
