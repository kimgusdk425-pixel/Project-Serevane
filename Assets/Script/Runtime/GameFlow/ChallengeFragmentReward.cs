using UnityEngine;

public class ChallengeFragmentReward : MonoBehaviour
{
    public event System.Action Revealed;
    [SerializeField] private TimedCollectionChallenge challenge;
    [SerializeField] private JumpFragmentPickup reward;
    [SerializeField] private GameObject rewardGroup; // 작은 빛 무리는 부모 오브젝트로 함께 공개
    private GameObject RewardObject => rewardGroup != null ? rewardGroup : (reward != null ? reward.gameObject : null);
    private bool revealed;
    private JumpFragmentPickup[] pickups;
    public Vector3 RewardPosition => RewardObject != null ? RewardObject.transform.position : transform.position;
    public bool HasAvailableReward
    {
        get
        {
            if (!revealed || RewardObject == null || !RewardObject.activeInHierarchy || pickups == null) return false;
            foreach (var pickup in pickups) if (pickup != null && !pickup.IsCollected) return true;
            return false;
        }
    }

    private void Awake()
    {
        if (RewardObject != null) { pickups = RewardObject.GetComponentsInChildren<JumpFragmentPickup>(true); RewardObject.SetActive(false); } // 성공 전에는 획득 불가
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
        if (revealed || RewardObject == null) return;
        revealed = true;
        RewardObject.SetActive(true); // 자동 강화가 아니라 직접 닿아야 획득
        Revealed?.Invoke(); // 실제 보상이 나타난 뒤 한 번만 알림
    }
}
