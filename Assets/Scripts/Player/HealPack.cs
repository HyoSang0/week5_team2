using UnityEngine;

/// <summary>
/// 힐팩 회복 컴포넌트. 트리거에 진입한 객체가 플레이어일 때만 회복을 시도하고 실제로 체력이 증가했을 때만 소멸한다.
/// </summary>
public class HealPack : MonoBehaviour, IHealingSource
{
    private const int HEALING_AMOUNT = 1;

    /// <summary>
    /// 힐팩 트리거에 진입한 콜라이더 상위 체인에서 PlayerHp를 찾아 플레이어 전용으로 회복을 시도한다.
    /// 진입한 other의 콜라이더를 사용하며, 회복에 성공해 실제로 체력이 증가했을 때만 힐팩을 파괴해 소비한다.
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        PlayerHp player = other.GetComponentInParent<PlayerHp>();
        if (player == null)
        {
            return;
        }

        IHealable healTarget = player;
        if (healTarget.ReceiveHealing(new HealingInfo(HEALING_AMOUNT, HealingKind.HealPack, this)))
        {
            Destroy(gameObject);
        }
    }
}
