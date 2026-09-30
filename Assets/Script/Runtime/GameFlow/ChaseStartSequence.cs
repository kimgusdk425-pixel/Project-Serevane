using System.Collections;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class ChaseStartSequence : MonoBehaviour
{
    [SerializeField] private TofuMonster monster; // 낙하와 추격을 모두 맡는 바위
    [SerializeField] private CameraFollow cameraFollow; // 충격과 반대편 구도
    [SerializeField, Min(0f)] private float fallHeight = 8f; // 바위의 시작 높이
    [SerializeField, Min(0.01f)] private float fallSeconds = 0.7f; // 떨어지는 시간
    [SerializeField, Min(0f)] private float impactPauseSeconds = 0.9f; // 충격 뒤 조작 잠금
    [SerializeField, Min(0f)] private float chaseDelaySeconds = 0.45f; // 조작 복구 뒤 여유
    [SerializeField, Min(0f)] private float bounceHeight = 0.45f; // 플레이어가 살짝 뜨는 높이

    private Vector3 landingPosition; // 씬에 둔 추격자의 착지 위치
    private CharacterController monsterController; // 낙하 중 충돌을 잠시 끔
    private Coroutine runningSequence;
    private bool hasStarted;

    private void Reset()
    {
        GetComponent<BoxCollider>().isTrigger = true; // 지나가면 시작하는 영역
    }

    private void Awake()
    {
        if (monster == null || cameraFollow == null)
        {
            Debug.LogError("ChaseStartSequence: 추격자와 카메라를 연결해야 합니다.", this);
            enabled = false;
            return;
        }

        landingPosition = monster.transform.position; // 바위 한 개의 착지점을 기억
        monsterController = monster.GetComponent<CharacterController>();
        ResetForRetry(); // 처음에는 바위를 올리고 추격을 대기시킴
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerMovement player = other.GetComponentInParent<PlayerMovement>();
        if (hasStarted || player == null)
        {
            return; // 플레이어가 처음 통과했을 때만 시작
        }

        hasStarted = true;
        runningSequence = StartCoroutine(DropAndChase(player));
    }

    private IEnumerator DropAndChase(PlayerMovement player)
    {
        Vector3 start = landingPosition + Vector3.up * fallHeight;
        float elapsed = 0f;

        while (elapsed < fallSeconds)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / fallSeconds);
            monster.transform.position = Vector3.Lerp(start, landingPosition, progress * progress); // 점점 빨라지는 낙하
            yield return null;
        }

        monster.transform.position = landingPosition;
        if (monsterController != null)
        {
            monsterController.enabled = true; // 착지 뒤부터 바위 충돌을 켬
        }

        player.SetControlEnabled(false); // 카메라가 뒤집힐 동안 방향 입력 잠금
        player.ApplyImpactBounce(bounceHeight); // 충격으로 짧게 위로 뜸
        cameraFollow.ShowThreat(monster.transform); // 플레이어 앞쪽에서 바위를 보여 줌
        cameraFollow.PlayImpactShake(0.28f, 0.18f);

        yield return new WaitForSeconds(impactPauseSeconds);
        cameraFollow.BeginChaseView(); // 오른쪽 옆에서 플레이어와 바위를 함께 보여 줌
        player.SetSideScrollControls(true); // 옆 구도가 된 순간부터 A/D 앞뒤, W/S 좌우
        player.SetControlEnabled(true); // 카메라와 조작이 모두 바뀐 뒤 입력 복구
        yield return new WaitForSeconds(chaseDelaySeconds);
        monster.enabled = true; // 같은 바위가 곧바로 추격자가 됨
        runningSequence = null;
    }

    public void ResetForRetry()
    {
        if (monster == null)
        {
            return;
        }

        if (runningSequence != null)
        {
            StopCoroutine(runningSequence); // 재시작 중 예전 낙하 연출 중단
            runningSequence = null;
        }

        hasStarted = false;
        monster.enabled = false;
        if (monsterController != null)
        {
            monsterController.enabled = false; // 공중에서는 플레이어와 충돌하지 않음
        }

        monster.transform.position = landingPosition + Vector3.up * fallHeight; // 바위 하나만 다시 올림
    }
}
