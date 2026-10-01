using UnityEngine;

public class ChallengeFragmentReward : MonoBehaviour
{
    [SerializeField] private TimedCollectionChallenge challenge;
    [SerializeField] private JumpFragmentPickup reward;
    private bool revealed;

    private void Awake()
    {
        if (reward != null) reward.gameObject.SetActive(false); // 성공 전에는 획득 불가
    }

    private void OnEnable()
    {
        if (challenge != null) challenge.Completed += Reveal;
    }

    private void OnDisable()
    {
        if (challenge != null) challenge.Completed -= Reveal; // 중복 구독과 해제 누락 방지
    }

    private void Reveal()
    {
        if (revealed || reward == null) return;
        revealed = true;
        reward.gameObject.SetActive(true); // 자동 강화가 아니라 직접 닿아야 획득
    }
}
