using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerMovement))]
public class PlayerFallRecovery : MonoBehaviour
{
    [SerializeField] private float minimumY = -60f;
    [SerializeField] private CameraFollow cameraFollow;
    [SerializeField] private ChaseRespawnSequence chaseRespawn;
    private PlayerMovement player;

    private void Awake()
    {
        player = GetComponent<PlayerMovement>();
    }

    private void LateUpdate()
    {
        if (!player.IsControlEnabled || transform.position.y >= minimumY) return; // 연출 중 낙하는 연출 담당이 복구
        if (chaseRespawn != null && chaseRespawn.TryBeginCatch()) return; // 추격자와 체크포인트를 함께 복원
        player.Respawn();
        if (cameraFollow != null) cameraFollow.SnapToTarget();
    }
}