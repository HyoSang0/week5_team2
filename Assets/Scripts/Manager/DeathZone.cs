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
            // 살아 있는 적의 첫 진입만 집계한다. DoDie가 isDead를 즉시 세우므로 중복 트리거·이미 사망한 적은 세지 않는다.
            if (!fallEnemy.isDead)
            {
                GameManager.Instance.RecordEnemyFall();
            }
            fallEnemy.DoDie(false);
        }
    }
}
