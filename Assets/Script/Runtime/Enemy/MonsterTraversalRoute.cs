using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class MonsterTraversalRoute : MonoBehaviour
{
    public enum TraversalState { Running, Falling, Jumping, Waiting, Blocked }
    [SerializeField] private Transform[] waypoints; // 발판 위 실제 도착 지점
    [SerializeField] private int[] jumpSteps; // 다음 발판으로 점프할 지점 번호
    [SerializeField, Min(0.1f)] private float runSpeed = 2.5f;
    [SerializeField, Min(0.1f)] private float jumpHeight = 1.8f;
    [SerializeField, Min(0.1f)] private float maxJumpSpeed = 3f; // 플레이어 공중 속도보다 느린 점프 추격
    [SerializeField] private Collider[] platforms; // 아래 착지 바닥부터 진행 순서대로
    [SerializeField] private int[] platformExitSteps; // 각 발판에서 다음 점프를 준비하는 경로 번호
    [SerializeField] private float gravity = -9.81f;
    [SerializeField, Min(0.05f)] private float arrivalDistance = 0.25f;
    [SerializeField] private Transform lowerRetryPoint;
    [SerializeField] private int lowerRetryStep = 1;
    private CharacterController body;
    private float verticalSpeed;
    private float originalStepOffset;
    private Vector3 jumpVelocity;
    private bool jumping;
    private bool launchedJump; // 한 경로 지점에서 착지 후 다시 뛰지 않도록 기록
    private float airborneSeconds;
    private int nextStep;
    private Transform retryPoint; // 이번 진행에서 활성화된 복귀 지점
    private int retryStep;
    private Transform chaseTarget;
    private CharacterController targetBody;
    private int targetPlatform = -1; // 점프 중에는 마지막으로 밟은 발판을 기억
    public bool IsFollowing { get; private set; }
    public bool HasLowerCheckpoint { get; private set; }
    public TraversalState State { get; private set; }
    public int NextStep => nextStep;

    private void Awake()
    {
        body = GetComponent<CharacterController>();
        originalStepOffset = body.stepOffset;
    }

    public void BeginFromCurrentPosition()
    {
        if (waypoints == null || waypoints.Length == 0) return;
        nextStep = 0;
        verticalSpeed = 0f;
        jumping = false;
        launchedJump = false;
        IsFollowing = true; // 위치를 바꾸지 않고 경로 이동만 시작
        HasLowerCheckpoint = false;
        targetPlatform = -1;
    }

    public void Tick(float deltaTime, Transform targetPlayer = null, float runMultiplier = 1f)
    {
        if (!IsFollowing || !body.enabled || deltaTime <= 0f) return;
        bool grounded = body.isGrounded;
        if (grounded && verticalSpeed <= 0f)
        {
            verticalSpeed = -2f;
            if (jumping && airborneSeconds > 0.1f) jumping = false;
        }
        runMultiplier = Mathf.Clamp(runMultiplier, 1f, 2f);
        if (FollowPlayerOnPlatform(targetPlayer, grounded, deltaTime, runMultiplier)) return;
        if (nextStep >= waypoints.Length || waypoints[nextStep] == null)
        {
            State = TraversalState.Waiting; // 경로 끝에서는 사라지지 않고 남음
            verticalSpeed += gravity * deltaTime;
            body.Move(Vector3.up * verticalSpeed * deltaTime);
            return;
        }
        Vector3 target = waypoints[nextStep].position;
        Vector3 horizontal = target - transform.position;
        horizontal.y = 0f;
        if (!launchedJump && grounded && IsJumpStep(nextStep))
        {
            float upwardSpeed = Mathf.Sqrt(jumpHeight * -2f * gravity);
            float heightDifference = target.y - transform.position.y;
            float discriminant = upwardSpeed * upwardSpeed + 2f * gravity * heightDifference;
            if (discriminant <= 0f)
            {
                WaitAtBlockedStep(deltaTime); // 강화된 플레이어만 넘는 높이
                return;
            }
            float flightSeconds = (upwardSpeed + Mathf.Sqrt(discriminant)) / -gravity;
            jumpVelocity = horizontal / flightSeconds; // 내려올 때 다음 발판에 도달할 속도
            if (jumpVelocity.sqrMagnitude > maxJumpSpeed * maxJumpSpeed)
            {
                WaitAtBlockedStep(deltaTime); // 멀리 있는 목적지로 비정상 가속하지 않음
                return;
            }
            verticalSpeed = upwardSpeed;
            jumping = true;
            launchedJump = true;
            airborneSeconds = 0f;
        }
        Vector3 velocity = jumping ? jumpVelocity
            : horizontal.normalized * Mathf.Min(runSpeed * (grounded ? runMultiplier : 1f), horizontal.magnitude / deltaTime);
        if (jumping) airborneSeconds += deltaTime;
        verticalSpeed += gravity * deltaTime;
        body.stepOffset = grounded && !jumping ? originalStepOffset : 0f;
        body.Move((velocity + Vector3.up * verticalSpeed) * deltaTime); // 충돌체를 유지한 이동과 중력
        State = jumping ? TraversalState.Jumping
            : body.isGrounded ? TraversalState.Running : TraversalState.Falling;
        Vector3 remaining = target - transform.position;
        remaining.y = 0f;
        if (remaining.sqrMagnitude <= arrivalDistance * arrivalDistance &&
            (nextStep == 0 || (body.isGrounded && !jumping)))
        {
            nextStep++;
            launchedJump = false;
        }
    }

    private bool IsJumpStep(int step)
    {
        if (jumpSteps == null) return false;
        foreach (int value in jumpSteps) if (value == step) return true;
        return false;
    }

    private bool FollowPlayerOnPlatform(Transform target, bool grounded, float deltaTime, float runMultiplier)
    {
        if (target == null || platforms == null || platformExitSteps == null ||
            platforms.Length != platformExitSteps.Length) return false; // 기존 경로만 있는 씬도 유지
        if (chaseTarget != target)
        {
            chaseTarget = target;
            targetBody = target.GetComponent<CharacterController>();
            targetPlatform = -1;
        }
        if (targetBody != null && targetBody.enabled && targetBody.isGrounded)
        {
            int found = FindPlatform(targetBody);
            if (found >= 0) targetPlatform = found;
        }
        if (!grounded || jumping || targetPlatform < 0) return false;
        int current = FindPlatform(body);
        if (current < 0) return false;
        if (nextStep < platformExitSteps[current])
        {
            nextStep = platformExitSteps[current];
            launchedJump = false; // 발판에 착지하면 다음 점프 준비
        }
        if (targetPlatform > current) return false; // 다음 발판에 도착했으면 경로 점프

        Vector3 velocity = Vector3.zero;
        if (current == targetPlatform)
        {
            Bounds floor = platforms[current].bounds;
            float margin = body.radius * Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.z)) + 0.05f;
            Vector3 destination = target.position;
            destination.x = Mathf.Clamp(destination.x, floor.min.x + margin, floor.max.x - margin);
            destination.z = Mathf.Clamp(destination.z, floor.min.z + margin, floor.max.z - margin);
            Vector3 towardPlayer = destination - transform.position;
            towardPlayer.y = 0f;
            velocity = towardPlayer.normalized * Mathf.Min(runSpeed * runMultiplier, towardPlayer.magnitude / deltaTime);
            State = TraversalState.Running;
        }
        else State = TraversalState.Waiting; // 플레이어를 앞질렀다면 다음 발판으로 계속 달리지 않음
        verticalSpeed = -2f;
        body.stepOffset = originalStepOffset;
        body.Move((velocity + Vector3.up * verticalSpeed) * deltaTime);
        return true;
    }

    public bool TryGetRunLead(Transform target, out float lead)
    {
        lead = 0f;
        if (!IsFollowing || !body.enabled || !body.isGrounded || jumping || target == null ||
            waypoints == null || waypoints.Length < 2 || nextStep <= 0 || nextStep >= waypoints.Length - 1 ||
            IsJumpStep(nextStep)) return false;
        CharacterController targetCharacter = target == chaseTarget ? targetBody : target.GetComponent<CharacterController>();
        if (targetCharacter == null || !targetCharacter.enabled || !targetCharacter.isGrounded) return false;
        if (platforms != null && platforms.Length > 0 && FindPlatform(targetCharacter) == platforms.Length - 1)
            return false; // 최종 발판에 도달한 플레이어는 거리 보정 제외
        lead = GetPathProgress(target.position) - GetPathProgress(transform.position);
        return true;
    }

    private float GetPathProgress(Vector3 position)
    {
        float accumulated = 0f;
        float bestProgress = 0f;
        float bestDistance = float.PositiveInfinity;
        for (int i = 1; i < waypoints.Length; i++)
        {
            if (waypoints[i - 1] == null || waypoints[i] == null) continue;
            Vector3 start = waypoints[i - 1].position;
            Vector3 segment = waypoints[i].position - start;
            float length = segment.magnitude;
            if (length <= 0.001f) continue;
            float ratio = Mathf.Clamp01(Vector3.Dot(position - start, segment) / segment.sqrMagnitude);
            float distance = (position - (start + segment * ratio)).sqrMagnitude;
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestProgress = accumulated + length * ratio; // 직선거리가 아닌 실제 경로의 진행 거리
            }
            accumulated += length;
        }
        return bestProgress;
    }

    private int FindPlatform(CharacterController character)
    {
        Bounds feet = character.bounds;
        for (int i = 0; i < platforms.Length; i++)
        {
            Collider floor = platforms[i];
            if (floor == null || !floor.enabled || !floor.gameObject.activeInHierarchy) continue;
            Bounds bounds = floor.bounds;
            if (feet.center.x >= bounds.min.x && feet.center.x <= bounds.max.x &&
                feet.center.z >= bounds.min.z && feet.center.z <= bounds.max.z &&
                Mathf.Abs(feet.min.y - bounds.max.y) <= 0.35f) return i; // 현재 블록맵의 수평 발판 판정
        }
        return -1;
    }

    private void WaitAtBlockedStep(float deltaTime)
    {
        State = TraversalState.Blocked;
        verticalSpeed = body.isGrounded ? -2f : verticalSpeed + gravity * deltaTime;
        body.Move(Vector3.up * verticalSpeed * deltaTime); // 통과 못 해도 바닥 충돌과 중력은 유지
    }

    public void RecordLowerCheckpoint()
    {
        RecordCheckpoint(lowerRetryPoint, lowerRetryStep);
    }

    public bool RecordCheckpoint(Transform point, int step)
    {
        if (!IsFollowing || point == null || waypoints == null ||
            step < 0 || step >= waypoints.Length) return false;
        retryPoint = point; // 현재 추격자 위치는 옮기지 않음
        retryStep = step;
        HasLowerCheckpoint = true;
        return true;
    }

    public bool ResetToCheckpoint()
    {
        if (!HasLowerCheckpoint || retryPoint == null) return false;
        body.enabled = false;
        transform.SetPositionAndRotation(retryPoint.position, retryPoint.rotation);
        body.enabled = true; // 실패 암전 중에만 복귀 위치 변경
        nextStep = Mathf.Clamp(retryStep, 0, waypoints.Length - 1);
        launchedJump = false;
        verticalSpeed = 0f;
        jumping = false;
        IsFollowing = true;
        targetPlatform = -1;
        return true;
    }

    public void ClearRoute()
    {
        launchedJump = false;
        IsFollowing = false;
        HasLowerCheckpoint = false;
        targetPlatform = -1;
        retryPoint = null;
        jumping = false;
        verticalSpeed = 0f;
        body.stepOffset = originalStepOffset;
    }
}
