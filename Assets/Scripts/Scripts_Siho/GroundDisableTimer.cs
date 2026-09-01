using System.Collections;
using UnityEngine;

public class GroundDisableTimer : MonoBehaviour
{
    private Renderer rend;
    private Collider col;

    private void Awake()
    {
        rend = GetComponent<Renderer>();
        col = GetComponent<Collider>();
    }

    public void DisableFor10Seconds()
    {
        StartCoroutine(DisableRoutine());
    }

    private IEnumerator DisableRoutine()
    {
        rend.enabled = false;
        col.enabled = false;

        yield return new WaitForSeconds(10f);

        rend.enabled = true;
        col.enabled = true;
    }
}
