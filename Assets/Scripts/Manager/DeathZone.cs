using UnityEngine;

public class DeathZone : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerFallRecovery recovery = other.GetComponent<PlayerFallRecovery>();
            recovery.HandleFall();
            return;
        }

        // 태그가 Enemy/NoAbsortEnemy로 나뉘어 있어 태그 대신 컴포넌트로 판정한다.
        Enemy fallEnemy = other.GetComponentInParent<Enemy>();
        if (fallEnemy != null)
        {
            fallEnemy.DoDie(false);
        }
    }
}
