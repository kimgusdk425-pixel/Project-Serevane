using System.Collections;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class ChaseFallTransition : MonoBehaviour
{
    [SerializeField] private TofuMonster monster;
    [SerializeField] private ChaseStartSequence chaseStart;
    [SerializeField] private ChaseRespawnSequence chaseRespawn;
    [SerializeField] private CameraFollow cameraFollow;
    [SerializeField] private Transform landingReturnPoint;
    private bool transitioning;

    private void OnTriggerEnter(Collider other)
    {
        PlayerMovement player = other.GetComponentInParent<PlayerMovement>();
        if (transitioning || player == null || monster == null ||
            cameraFollow == null || landingReturnPoint == null) return;
        transitioning = true;
        if (chaseStart != null) { chaseStart.StopAllCoroutines(); chaseStart.enabled = false; }
        if (!monster.BeginTraversal())
        {
            transitioning = false;
            Debug.LogError("ChaseFallTransition: 추격자 낙하 경로를 연결해야 합니다.", this);
            return;
        }
        cameraFollow.BeginTraversalView(monster.transform); // 떨어지는 동안부터 넓은 옆 구도로 전환
        player.SetSideScrollControls(true); // 착지 후에도 D 전진 유지
        player.SetControlEnabled(false); // 플레이어 중력은 계속 작동
        StartCoroutine(FallTogether(player));
    }

    private IEnumerator FallTogether(PlayerMovement player)
    {
        CharacterController body = player.GetComponent<CharacterController>();
        float elapsed = 0f;
        while (elapsed < 5f)
        {
            elapsed += Time.deltaTime;
            bool reachedLowerFloor = player.transform.position.y <= landingReturnPoint.position.y + 0.5f;
            if (elapsed > 0.4f && body.isGrounded && reachedLowerFloor) break; // 위쪽 바닥 접촉을 착지로 오인하지 않음
            yield return null;
        }
        player.SetRespawnPoint(landingReturnPoint.position, landingReturnPoint.rotation);
        if (!body.isGrounded || player.transform.position.y > landingReturnPoint.position.y + 0.5f)
            player.Respawn(); // 낙하가 막히거나 착지를 놓친 경우 안전한 도착점
        monster.RecordTraversalCheckpoint(); // 현재 위치는 유지하고 복귀 설정만 기억
        player.SetSideScrollControls(true); // 구간 중간에 앞 방향이 W로 바뀌지 않게 함
        player.SetControlEnabled(true);
    }

    public void ResetForRetry()
    {
        StopAllCoroutines();
        transitioning = false;
        if (chaseStart != null) chaseStart.enabled = true;
    }
}
