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
        if (chaseRespawn != null) { chaseRespawn.StopAllCoroutines(); chaseRespawn.enabled = false; }
        monster.enabled = false; // 낙하 중 다시 잡힘 판정을 하지 않음
        monster.GetComponent<CharacterController>().enabled = false;
        monster.transform.position = player.transform.position + new Vector3(2.5f, 0.2f, -2f);
        player.SetControlEnabled(false); // 플레이어 중력은 계속 작동
        StartCoroutine(FallTogether(player));
    }

    private IEnumerator FallTogether(PlayerMovement player)
    {
        CharacterController body = player.GetComponent<CharacterController>();
        float velocity = -2f;
        float elapsed = 0f;
        while (elapsed < 5f)
        {
            elapsed += Time.deltaTime;
            velocity -= 9.81f * Time.deltaTime;
            monster.transform.position += Vector3.up * velocity * Time.deltaTime;
            if (elapsed > 0.4f && body.isGrounded) break; // 아래 받침에 실제 착지한 뒤 구도 복구
            yield return null;
        }
        monster.gameObject.SetActive(false); // 바위는 아래로 사라지고 추격은 종료
        player.SetRespawnPoint(landingReturnPoint.position, landingReturnPoint.rotation);
        if (!body.isGrounded) player.Respawn(); // 낙하가 막히거나 착지를 놓친 경우 안전한 도착점
        player.SetSideScrollControls(false);
        cameraFollow.SnapToTarget();
        player.SetControlEnabled(true);
    }
}
