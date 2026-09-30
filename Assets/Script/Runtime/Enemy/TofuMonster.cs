using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class TofuMonster : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 2f; // 추적 속도
    [SerializeField] private float catchDistance = 1f; // 접촉 판정 거리
    [SerializeField] private bool resetOnCatch; // 잡히면 적도 출발점으로
    [SerializeField] private ChaseRespawnSequence catchSequence; // 추격 씬의 재시작 연출
    [SerializeField] private Transform rollingVisual; // 바위 외형만 굴리기
    [SerializeField] private float rollDegreesPerMeter = 48f; // 반지름 1.2 바위의 자연스러운 굴림량

    private CharacterController controller; // 충돌+이동 담당
    private Transform player; // 추적 대상
    private PlayerMovement playerMovement; // 리스폰 호출용
    private Vector3 spawnPosition; // 적의 처음 위치
    private Quaternion spawnRotation; // 적의 처음 방향
    private Quaternion visualSpawnRotation; // 재도전 때 바위 무늬 방향

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        spawnPosition = transform.position;
        spawnRotation = transform.rotation;
        if (rollingVisual != null)
        {
            visualSpawnRotation = rollingVisual.localRotation;
        }
    }

    private void Start()
    {
        PlayerMovement p = FindFirstObjectByType<PlayerMovement>(); // 플레이어 탐색
        if (p != null) // 찾았을 때만
        {
            player = p.transform;
            playerMovement = p;
        }
    }

    private void Update()
    {
        if (player == null) // 대상 없으면 정지
        {
            return;
        }

        Vector3 toPlayer = player.position - transform.position; // 추적 벡터
        toPlayer.y = 0f; // 수평 추적

        if (toPlayer.sqrMagnitude > 0.001f) // 방향 있을 때만
        {
            transform.rotation = Quaternion.LookRotation(toPlayer.normalized); // 응시
        }

        Vector3 movement = toPlayer.normalized * moveSpeed; // 등속 추적
        Vector3 beforeMove = transform.position;
        controller.Move(movement * Time.deltaTime); // 벽 슬라이딩 이동
        if (rollingVisual != null)
        {
            float distance = Vector3.Distance(beforeMove, transform.position);
            rollingVisual.Rotate(Vector3.right, distance * rollDegreesPerMeter, Space.Self); // 실제 이동한 만큼 굴림
        }

        if (Vector3.Distance(transform.position, player.position) <= catchDistance) // 접촉
        {
            if (catchSequence != null && catchSequence.TryBeginCatch())
            {
                return; // 화면 전환을 시작했다면 즉시 순간 이동하지 않음
            }

            playerMovement.Respawn(); // 시작으로 복귀

            if (resetOnCatch)
            {
                ResetToSpawn();
            }
        }
    }

    public void ResetToSpawn()
    {
        controller.enabled = false; // 순간 이동 중 충돌 보정 방지
        transform.SetPositionAndRotation(spawnPosition, spawnRotation);
        controller.enabled = true;
        if (rollingVisual != null)
        {
            rollingVisual.localRotation = visualSpawnRotation; // 새 시도는 처음 굴림 상태로
        }
    }
}
