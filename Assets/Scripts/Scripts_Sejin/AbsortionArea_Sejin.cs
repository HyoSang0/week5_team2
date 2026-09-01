using UnityEngine;
using UnityEngine.Events;

public class AbsortionArea_Sejin : MonoBehaviour
{
    public UnityEvent onGatherEnergy;
    void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.CompareTag("Enemy"))
        {
            onGatherEnergy.Invoke();
            // 슬로우
            // 소량 처치
            Destroy(other.gameObject);
        }
    }
}
