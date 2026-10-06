using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 3f; // 이동 속도
    [SerializeField, Min(1f)] private float airSpeedMultiplier = 1.1f; // 공중에서는 조금 더 멀리 이동
    [SerializeField] private float gravity = -9.81f; // 중력 값
    [SerializeField] private float jumpHeight = 1.2f; // 점프 높이
    [SerializeField] private float turnSpeed = 10f; // 회전 속도
    [SerializeField, Min(0f)] private float coyoteSeconds = 0.1f; // 발판을 벗어난 직후에도 점프 허용
    [SerializeField, Min(0f)] private float jumpBufferSeconds = 0.1f; // 착지 직전 누른 점프를 잠깐 기억
    [SerializeField] private bool useStrictSurfaceCollision; // 지정 발판의 모서리 타기를 막을 씬에서만 사용
    private bool useSideScrollControls; // 옆 카메라로 바뀐 뒤에만 추격 조작 사용

    private CharacterController characterController; // 충돌+이동 담당
    private PlayerCarryController carryController; // 들고 있는 상자의 앞 공간 검사
    private PlayerJumpProgress jumpProgress; // 수집한 빛 조각의 점프 보너스
    private InputSystem_Actions inputActions; // 자동 발급 입력표
    private Camera mainCam; // 기준 카메라
    private float verticalVelocity; // 떨어지는 속도
    private float groundedStepOffset; // 바닥에서 작은 턱을 넘는 원래 높이
    private Vector3 spawnPos; // 시작 위치
    private Quaternion spawnRotation;
    private bool canControl = true; // 이동·점프 입력 허용 여부
    private float lastGroundedTime = float.NegativeInfinity;
    private float lastJumpPressedTime = float.NegativeInfinity;
    private bool canUseGroundJump; // 한 번 떠오르면 착지 전까지 추가 점프 금지
    private readonly RaycastHit[] surfaceHits = new RaycastHit[32]; // 매 프레임 새 배열 생성 방지

    public float CurrentJumpHeight => jumpHeight + (jumpProgress != null ? jumpProgress.JumpBonus : 0f);
    public bool IsControlEnabled => canControl && isActiveAndEnabled; // 다른 행동도 같은 조작 잠금을 확인

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        groundedStepOffset = characterController.stepOffset;
        carryController = GetComponent<PlayerCarryController>();
        jumpProgress = GetComponent<PlayerJumpProgress>();
        inputActions = new InputSystem_Actions(); // 입력표 생성
        mainCam = Camera.main; // 메인 카메라 자동 탐색
        spawnPos = transform.position; // 시작 위치 기억
        spawnRotation = transform.rotation;
    }

    private void OnEnable()
    {
        inputActions.Player.Enable(); // Player 맵 켜기
    }

    private void OnDisable()
    {
        inputActions.Player.Disable(); // Player 맵 끄기
        characterController.stepOffset = groundedStepOffset; // 다른 이동 담당으로 넘길 때 원래 값 복원
        ClearJumpGrace();
    }

    private void OnDestroy()
    {
        inputActions.Dispose(); // 입력표 정리
    }

    private void Update()
    {
        Vector2 input = canControl
            ? Vector2.ClampMagnitude(inputActions.Player.Move.ReadValue<Vector2>(), 1f)
            : Vector2.zero; // 종료 후에는 입력만 무시
        Vector3 moveDir = ToCameraSpace(input); // 카메라 기준 방향
        bool grounded = characterController.isGrounded;
        if (grounded && verticalVelocity < 0f) verticalVelocity = -2f; // 바닥에 붙이기
        bool startedJump = TryJump(grounded, canControl && inputActions.Player.Jump.WasPressedThisFrame(), Time.time);
        bool jumping = !grounded || startedJump;
        Vector3 movement = moveDir * moveSpeed * (jumping ? airSpeedMultiplier : 1f); // 점프 시작부터 공중 속도 적용

        if (moveDir.sqrMagnitude > 0.001f) // 입력 있을 때만
        {
            Quaternion look = Quaternion.LookRotation(moveDir); // 바라볼 방향
            Quaternion nextRotation = Quaternion.Slerp(transform.rotation, look, turnSpeed * Time.deltaTime);
            if (carryController == null || carryController.CanMoveHeldBox(Vector3.zero, nextRotation))
            {
                transform.rotation = nextRotation; // 상자가 벽을 통과하지 않을 때만 회전
            }
        }

        verticalVelocity += gravity * Time.deltaTime; // 낙하 가속

        movement.y = verticalVelocity; // 상하 속도 합치기
        Vector3 step = movement * Time.deltaTime;
        if (carryController != null && !carryController.CanMoveHeldBox(step, transform.rotation))
        {
            step = new Vector3(0f, step.y, 0f); // 벽에 닿았으면 수평 이동만 취소
            if (!carryController.CanMoveHeldBox(step, transform.rotation))
            {
                step = Vector3.zero; // 천장처럼 위아래도 막혔으면 그 자리 유지
                if (verticalVelocity > 0f)
                {
                    verticalVelocity = 0f;
                }
            }
        }

        characterController.stepOffset = characterController.isGrounded && verticalVelocity <= 0f
            ? groundedStepOffset : 0f; // 공중에서는 작은 턱 넘기로 점프 높이가 더해지지 않게 함
        if (useStrictSurfaceCollision) step = LimitNoClimbMovement(step); // 기존 추격 씬에는 추가 검사하지 않음
        characterController.Move(step); // 플레이어 몸과 들고 있는 상자 모두 통과하지 않을 때 이동
    }

    private bool TryJump(bool grounded, bool pressed, float now)
    {
        if (!canControl) { ClearJumpGrace(); return false; }
        if (grounded && verticalVelocity <= 0f)
        {
            lastGroundedTime = now;
            canUseGroundJump = true;
        }
        if (pressed) lastJumpPressedTime = now;
        if (!canUseGroundJump || now - lastGroundedTime > coyoteSeconds ||
            now - lastJumpPressedTime > jumpBufferSeconds) return false;
        verticalVelocity = Mathf.Sqrt(CurrentJumpHeight * -2f * gravity);
        ClearJumpGrace(); // 입력 여유를 사용해도 공중 추가 점프는 금지
        return true;
    }

    private void ClearJumpGrace()
    {
        lastGroundedTime = float.NegativeInfinity;
        lastJumpPressedTime = float.NegativeInfinity;
        canUseGroundJump = false;
    }

    private Vector3 LimitNoClimbMovement(Vector3 step)
    {
        Vector3 horizontal = new Vector3(step.x, 0f, step.z);
        float distance = horizontal.magnitude;
        if (distance < 0.0001f) return step;

        Bounds body = characterController.bounds;
        float padding = 0.01f;
        float sideInset = characterController.skinWidth + padding; // 몸 충돌의 허용 오차만큼 옆면을 줄임
        Vector3 halfSize = new Vector3(
            Mathf.Max(0.01f, body.extents.x - sideInset),
            Mathf.Max(0.01f, body.extents.y - padding),
            Mathf.Max(0.01f, body.extents.z - sideInset)); // 발끝 가까이까지 있는 사각형: 캡슐 모서리 상승 차단
        Vector3 direction = horizontal / distance;
        int count = Physics.BoxCastNonAlloc(body.center, halfSize, direction, surfaceHits,
            Quaternion.identity, distance + padding, Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore);
        float allowed = distance;
        for (int i = 0; i < count; i++)
        {
            RaycastHit hit = surfaceHits[i];
            NoClimbSurface surface = hit.collider.GetComponentInParent<NoClimbSurface>();
            if (hit.collider == characterController || surface == null || !surface.isActiveAndEnabled) continue; // 걸을 수 있는 바위는 일반 충돌로 처리
            Vector3 towardSurface = hit.collider.bounds.ClosestPoint(body.center) - body.center; // 오목한 지형 MeshCollider도 오류 없이 이탈 방향 확인
            towardSurface.y = 0f;
            if (hit.distance <= padding && towardSurface.sqrMagnitude > 0.0001f &&
                Vector3.Dot(direction, towardSurface) <= 0f) continue; // 밀착 상태에서도 뒤로 빠져나오기 허용
            if (Vector3.Dot(direction, hit.normal) >= -0.001f) continue; // 벽에서 멀어지는 이동은 허용
            allowed = Mathf.Min(allowed, Mathf.Max(0f, hit.distance - padding));
        }

        if (count == surfaceHits.Length) allowed = 0f; // 밀집 구역에서 결과 누락 시 통과보다 정지 선택
        Vector3 safeHorizontal = direction * allowed;
        return new Vector3(safeHorizontal.x, step.y, safeHorizontal.z); // 점프·낙하는 유지
    }

    public void SetControlEnabled(bool enabled)
    {
        canControl = enabled; // 중력은 유지하고 조작만 잠그기
        if (!enabled) ClearJumpGrace(); // 연출 전 입력이 조작 복구 직후 발동하지 않음
    }

    public void SetSideScrollControls(bool enabled)
    {
        useSideScrollControls = enabled; // 연출 전후의 조작 방식을 전환
    }

    public void ApplyImpactBounce(float height)
    {
        if (height <= 0f)
        {
            return;
        }

        float bounceSpeed = Mathf.Sqrt(height * -2f * gravity); // 원하는 높이를 위쪽 속도로 변환
        verticalVelocity = Mathf.Max(verticalVelocity, bounceSpeed); // 이미 뛰었다면 더 약하게 만들지 않음
        ClearJumpGrace(); // 충격으로 뜬 뒤 추가 점프 방지
    }

    public void SetRespawnPoint(Vector3 position, Quaternion rotation)
    {
        spawnPos = position; // 퍼즐 진행은 유지하고 돌아올 위치만 변경
        spawnRotation = rotation;
    }

    public void Respawn()
    {
        characterController.enabled = false; // 충돌 잠시 해제
        transform.SetPositionAndRotation(spawnPos, spawnRotation); // 체크포인트 위치와 방향으로
        verticalVelocity = 0f; // 낙하 속도 초기화
        ClearJumpGrace(); // 재시작 전 점프 입력을 가져오지 않음
        characterController.enabled = true; // 충돌 복구
    }

    private Vector3 ToCameraSpace(Vector2 input)
    {
        if (useSideScrollControls)
        {
            return new Vector3(-input.y, 0f, input.x); // D=길 앞(+Z), A=뒤, W=왼(-X), S=오른
        }

        Vector3 fwd = Vector3.forward; // 예비: 월드 앞
        Vector3 right = Vector3.right; // 예비: 월드 옆

        if (mainCam != null) // 카메라 있으면 기준 교체
        {
            fwd = mainCam.transform.forward; // 카메라 앞
            fwd.y = 0f; // 수평 투영
            fwd.Normalize(); // 길이 1
            right = mainCam.transform.right; // 카메라 옆
            right.y = 0f; // 수평 투영
            right.Normalize(); // 길이 1
        }

        return right * input.x + fwd * input.y; // 카메라 기준 합성
    }
}
