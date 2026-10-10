using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class TofuMonster : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 2f; // 추적 속도
    [SerializeField] private float catchDistance = 1f; // 몸 충돌체가 없는 대상의 예비 판정 거리
    [SerializeField] private bool resetOnCatch; // 잡히면 적도 출발점으로
    [SerializeField] private ChaseRespawnSequence catchSequence; // 추격 씬의 재시작 연출
    [SerializeField] private Transform rollingVisual; // 바위 외형만 굴리기
    [SerializeField] private float rollDegreesPerMeter = 48f; // 반지름 1.2 바위의 자연스러운 굴림량
    [SerializeField] private bool useDistanceCorrection; // MainGame 추격에서만 거리 보정 사용
    [SerializeField, Min(0f)] private float correctionStartDistance = 8f; // 이 거리 안에서는 원래 속도
    [SerializeField, Min(0.1f)] private float correctionFullDistance = 20f;
    [SerializeField, Range(1f, 2f)] private float maxSpeedMultiplier = 1.4f;
    [SerializeField, Min(0.01f)] private float multiplierChangeRate = 0.4f; // 갑자기 빨라지지 않도록 변화 제한
    private float speedMultiplier = 1f;
    public float CurrentSpeedMultiplier => speedMultiplier;

    private CharacterController controller; // 충돌+이동 담당
    private Transform player; // 추적 대상
    private PlayerMovement playerMovement; // 리스폰 호출용
    private CharacterController playerController; // 실제 몸 크기로 접촉 판정
    private Vector3 spawnPosition; // 적의 처음 위치
    private Quaternion spawnRotation; // 적의 처음 방향
    private Quaternion visualSpawnRotation; // 재도전 때 바위 무늬 방향
    private MonsterGroundTraversal groundTraversal; // 밝은 구간의 바위 넘기와 중력
    private MonsterTraversalRoute traversalRoute; // 경로가 있는 MainGame만 점프 추격
    public bool HasTraversalCheckpoint => traversalRoute != null && traversalRoute.HasLowerCheckpoint;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        traversalRoute = GetComponent<MonsterTraversalRoute>();
        groundTraversal = GetComponent<MonsterGroundTraversal>();
        spawnPosition = transform.position;
        spawnRotation = transform.rotation;
        if (rollingVisual != null)
        {
            visualSpawnRotation = rollingVisual.localRotation;
        }
    }

    private void OnEnable()
    {
        if (groundTraversal != null) groundTraversal.ResetMotion();
        speedMultiplier = 1f; // 재시작 후 이전 가속을 가져오지 않음
    }

    private void Start()
    {
        PlayerMovement p = FindFirstObjectByType<PlayerMovement>(); // 플레이어 탐색
        if (p != null) // 찾았을 때만
        {
            player = p.transform;
            playerMovement = p;
            playerController = p.GetComponent<CharacterController>();
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

        UpdateDistanceCorrection(Time.deltaTime);
        Vector3 movement = toPlayer.normalized * moveSpeed * speedMultiplier;
        Vector3 beforeMove = transform.position;
        if (traversalRoute != null && traversalRoute.IsFollowing)
            traversalRoute.Tick(Time.deltaTime, player, speedMultiplier); // 점프 속도는 경로가 유지하고 달리기만 보정
        else if (groundTraversal != null && groundTraversal.isActiveAndEnabled)
            groundTraversal.Tick(movement, Time.deltaTime);
        else controller.Move(movement * Time.deltaTime); // 기존 평지 추격 유지
        if (rollingVisual != null)
        {
            float distance = Vector3.Distance(beforeMove, transform.position);
            rollingVisual.Rotate(Vector3.right, distance * rollDegreesPerMeter, Space.Self); // 실제 이동한 만큼 굴림
        }

        if (playerMovement != null && playerMovement.IsControlEnabled && IsTouchingPlayer()) // 연출 중 중복 잡힘 방지
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

    private void UpdateDistanceCorrection(float deltaTime)
    {
        if (!useDistanceCorrection || playerMovement == null || !playerMovement.IsControlEnabled ||
            playerController == null || !playerController.enabled || !controller.enabled || !playerController.isGrounded)
        {
            speedMultiplier = 1f; // 낙하·점프·암전·종료 중에는 가속하지 않음
            return;
        }

        float lead;
        if (traversalRoute != null && traversalRoute.IsFollowing)
        {
            if (!traversalRoute.TryGetRunLead(player, out lead))
            {
                speedMultiplier = 1f; // 점프 준비·공중·최종 탈출 지점은 보정 제외
                return;
            }
        }
        else
        {
            if (Mathf.Abs(playerController.bounds.min.y - controller.bounds.min.y) > 0.35f)
            {
                speedMultiplier = 1f;
                return;
            }
            Vector3 forward = spawnRotation * Vector3.forward;
            forward.y = 0f;
            lead = Vector3.Dot(player.position - transform.position, forward.normalized); // 평지 시작 방향의 진행 차이
        }

        float fullDistance = Mathf.Max(correctionStartDistance + 0.1f, correctionFullDistance);
        float amount = Mathf.InverseLerp(correctionStartDistance, fullDistance, lead);
        float desired = Mathf.Lerp(1f, Mathf.Clamp(maxSpeedMultiplier, 1f, 2f), amount);
        speedMultiplier = Mathf.MoveTowards(speedMultiplier, desired,
            Mathf.Max(0.01f, multiplierChangeRate) * Mathf.Max(0f, deltaTime)); // 가속과 감속을 모두 부드럽게
    }

    private bool IsTouchingPlayer()
    {
        if (playerController == null)
            return (transform.position - player.position).sqrMagnitude <= catchDistance * catchDistance;
        if (!controller.enabled || !playerController.enabled) return false; // 순간 이동 중에는 접촉하지 않음

        Vector3 enemyCenter = transform.TransformPoint(controller.center);
        Vector3 playerCenter = player.TransformPoint(playerController.center);
        Vector3 enemyScale = transform.lossyScale;
        Vector3 playerScale = player.lossyScale;
        float enemyRadius = controller.radius * Mathf.Max(Mathf.Abs(enemyScale.x), Mathf.Abs(enemyScale.z));
        float playerRadius = playerController.radius * Mathf.Max(Mathf.Abs(playerScale.x), Mathf.Abs(playerScale.z));
        float enemyHalfLine = Mathf.Max(0f, controller.height * Mathf.Abs(enemyScale.y) * 0.5f - enemyRadius);
        float playerHalfLine = Mathf.Max(0f, playerController.height * Mathf.Abs(playerScale.y) * 0.5f - playerRadius);

        Vector3 gap = playerCenter - enemyCenter;
        gap.y = Mathf.Max(0f, Mathf.Abs(gap.y) - enemyHalfLine - playerHalfLine); // 똑바로 선 캡슐의 가운데 선끼리 세로 간격
        float tolerance = Mathf.Max(controller.skinWidth, playerController.skinWidth) + 0.01f; // 물리가 남기는 작은 접촉 오차 허용
        float touchingDistance = enemyRadius + playerRadius + tolerance;
        return gap.sqrMagnitude <= touchingDistance * touchingDistance; // 몸 표면이 닿으면 잡힘: 높이 차이도 확인
    }

    public void ResetToSpawn()
    {
        if (groundTraversal != null) groundTraversal.ResetMotion();
        speedMultiplier = 1f;
        if (traversalRoute != null && traversalRoute.ResetToCheckpoint()) return;
        if (traversalRoute != null) traversalRoute.ClearRoute();
        controller.enabled = false; // 순간 이동 중 충돌 보정 방지
        transform.SetPositionAndRotation(spawnPosition, spawnRotation);
        controller.enabled = true;
        if (rollingVisual != null)
        {
            rollingVisual.localRotation = visualSpawnRotation; // 새 시도는 처음 굴림 상태로
        }
    }

    public bool BeginTraversal()
    {
        if (traversalRoute == null) return false;
        if (groundTraversal != null) groundTraversal.ResetMotion();
        traversalRoute.BeginFromCurrentPosition();
        controller.enabled = true;
        enabled = true;
        return traversalRoute.IsFollowing;
    }

    public void RecordTraversalCheckpoint()
    {
        if (traversalRoute != null) traversalRoute.RecordLowerCheckpoint();
    }
}
