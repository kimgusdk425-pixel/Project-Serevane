using UnityEngine;
using UnityEngine.InputSystem;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Vector3 offset = new Vector3(0f, 5f, -7f); // 추적 거리
    [SerializeField] private float smooth = 5f; // 추적 속도
    [SerializeField] private Vector3 lookOffset = new Vector3(0f, 1f, 0f); // 응시 높이
    [SerializeField] private Vector3 threatViewOffset = new Vector3(0f, 4f, 6f); // 뒤를 돌아보는 구도
    [SerializeField] private Vector3 chaseSideOffset = new Vector3(11f, 5.5f, -1.5f); // D가 화면 오른쪽인 횡스크롤 구도
    [SerializeField] private Vector3 traversalSideOffset = new Vector3(20f, 7f, 0f); // 낙하·점프 구간은 더 멀리서 발판까지 보여 줌
    [SerializeField] private bool enableMouseOrbit; // MainGame 탐험에서만 켜는 마우스 시점
    [SerializeField, Min(0.01f)] private float mouseSensitivity = 0.12f; // 마우스 1픽셀당 회전 각도
    [SerializeField] private float minPitch = 5f; // 바닥 아래로 카메라가 내려가지 않도록 제한
    [SerializeField] private float maxPitch = 65f; // 머리 바로 위에서 내려다보는 구도 제한
    [SerializeField, Min(0.1f)] private float orbitSmooth = 12f;
    [SerializeField, Min(0.1f)] private float rotationSmooth = 10f;
    [SerializeField, Min(0.01f)] private float collisionRadius = 0.3f; // 점이 아닌 작은 구로 벽 검사
    [SerializeField, Min(0f)] private float collisionPadding = 0.08f;
    [SerializeField] private LayerMask cameraObstacles = Physics.DefaultRaycastLayers;

    private Transform target; // 추적 대상
    private Transform threat; // 충격 순간 보여 줄 바위
    private ViewMode viewMode; // 평상시·충격·추격 카메라 구분
    private float shakeRemaining; // 충격 연출이 남은 시간
    private float shakeStrength; // 카메라 흔들림 크기
    private PlayerMovement playerMovement;
    private float wantedYaw, wantedPitch, yaw, pitch, orbitDistance;
    private readonly RaycastHit[] cameraHits = new RaycastHit[32]; // 검사할 때마다 배열 생성하지 않음
    private bool cursorOwned;
    private CursorLockMode previousCursorLock;
    private bool previousCursorVisible;
    public bool IsChaseViewSettled => target != null && viewMode == ViewMode.ChaseSide &&
        Vector3.Distance(transform.position, target.position + chaseSideOffset) <= 0.5f;

    private enum ViewMode
    {
        Follow,
        ThreatReveal,
        ChaseSide,
        TraversalSide
    }

    private void Awake()
    {
        PlayerMovement p = FindFirstObjectByType<PlayerMovement>(); // 플레이어 자동 탐색
        if (p != null) // 찾았을 때만
        {
            target = p.transform;
            playerMovement = p;
        }
        ResetOrbitAngles();
    }

    private void OnEnable()
    {
        if (!enableMouseOrbit) return;
        previousCursorLock = Cursor.lockState;
        previousCursorVisible = Cursor.visible;
        cursorOwned = true;
        CaptureCursor();
    }

    private void OnDisable()
    {
        if (!cursorOwned) return;
        Cursor.lockState = previousCursorLock;
        Cursor.visible = previousCursorVisible;
        cursorOwned = false; // Play 종료 때 커서를 잠긴 채 남기지 않음
    }

    private void OnApplicationFocus(bool focused)
    {
        if (!focused && cursorOwned) ReleaseCursorForUI();
    }

    private void Update()
    {
        if (!enableMouseOrbit || Mouse.current == null) return;
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            ReleaseCursorForUI(); // Esc로 마우스를 Unity 창 밖으로 꺼낼 수 있음
            return;
        }
        if (Cursor.lockState != CursorLockMode.Locked)
        {
            bool overUI = UnityEngine.EventSystems.EventSystem.current != null &&
                UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
            if (Application.isFocused && Mouse.current.leftButton.wasPressedThisFrame && !overUI &&
                playerMovement != null && playerMovement.IsControlEnabled) CaptureCursor();
            return; // 다시 클릭한 프레임의 큰 마우스 이동은 버림
        }
        ApplyLookDelta(Mouse.current.delta.ReadValue());
    }

    private void ApplyLookDelta(Vector2 delta)
    {
        if (!enableMouseOrbit || viewMode != ViewMode.Follow || playerMovement == null ||
            !playerMovement.IsControlEnabled) return; // 추격·연출 중에는 마우스가 구도를 바꾸지 못함
        wantedYaw = Mathf.Repeat(wantedYaw + delta.x * mouseSensitivity, 360f);
        wantedPitch = Mathf.Clamp(wantedPitch - delta.y * mouseSensitivity,
            Mathf.Min(minPitch, maxPitch), Mathf.Max(minPitch, maxPitch));
    }

    private void CaptureCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void ReleaseCursorForUI()
    {
        if (!enableMouseOrbit) return;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void ResetOrbitAngles()
    {
        Vector3 relative = offset - lookOffset;
        orbitDistance = Mathf.Max(0.1f, relative.magnitude);
        wantedYaw = yaw = Mathf.Atan2(-relative.x, -relative.z) * Mathf.Rad2Deg;
        wantedPitch = pitch = Mathf.Clamp(Mathf.Atan2(relative.y,
            new Vector2(relative.x, relative.z).magnitude) * Mathf.Rad2Deg,
            Mathf.Min(minPitch, maxPitch), Mathf.Max(minPitch, maxPitch));
    }

    private Vector3 GetFollowOffset(float deltaTime)
    {
        if (!enableMouseOrbit) return offset;
        float t = 1f - Mathf.Exp(-orbitSmooth * deltaTime);
        yaw = Mathf.LerpAngle(yaw, wantedYaw, t); // 359도에서 0도로 회전할 때 한 바퀴 돌지 않음
        pitch = Mathf.Lerp(pitch, wantedPitch, t);
        return lookOffset + Quaternion.Euler(pitch, yaw, 0f) * Vector3.back * orbitDistance;
    }

    private Vector3 ResolveCameraPosition(Vector3 desired)
    {
        if (!enableMouseOrbit || viewMode != ViewMode.Follow) return desired; // 횡스크롤 구도는 기존 거리 유지
        Vector3 origin = target.position + lookOffset;
        Vector3 direction = desired - origin;
        float distance = direction.magnitude;
        if (distance < 0.001f) return desired;
        direction /= distance;
        int count = Physics.SphereCastNonAlloc(origin, collisionRadius, direction, cameraHits,
            distance, cameraObstacles, QueryTriggerInteraction.Ignore);
        float allowed = distance;
        for (int i = 0; i < count; i++)
        {
            Collider hit = cameraHits[i].collider;
            if (hit == null || hit.transform.IsChildOf(target)) continue; // 자기 몸·들고 있는 상자는 제외
            allowed = Mathf.Min(allowed, Mathf.Max(0f, cameraHits[i].distance - collisionPadding));
        }
        if (count == cameraHits.Length) allowed = 0f; // 검사 결과가 꽉 차면 벽 통과보다 안전한 위치 선택
        return origin + direction * allowed;
    }

    private void LateUpdate()
    {
        if (target == null) // 대상 없으면 정지
        {
            return;
        }

        Vector3 viewOffset = viewMode switch
        {
            ViewMode.ThreatReveal => threatViewOffset,
            ViewMode.ChaseSide => chaseSideOffset,
            ViewMode.TraversalSide => GetTraversalOffset(),
            _ => GetFollowOffset(Time.deltaTime)
        };
        Vector3 want = target.position + viewOffset; // 현재 구도의 위치
        want = ResolveCameraPosition(want);
        float t = 1f - Mathf.Exp(-smooth * Time.deltaTime); // 프레임 독립 보간
        Vector3 nextPosition = Vector3.Lerp(transform.position, want, t); // 부드럽게 추적
        if (shakeRemaining > 0f)
        {
            shakeRemaining = Mathf.Max(0f, shakeRemaining - Time.deltaTime);
            nextPosition += Random.insideUnitSphere * shakeStrength; // 짧은 충격 흔들림
        }

        transform.position = ResolveCameraPosition(nextPosition); // 보간 경로가 모퉁이를 가로질러도 다시 벽 검사
        Vector3 lookDirection = GetLookPoint() - transform.position;
        if (lookDirection.sqrMagnitude > 0.0001f)
        {
            Quaternion desiredRotation = Quaternion.LookRotation(lookDirection);
            float rotationT = 1f - Mathf.Exp(-rotationSmooth * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, rotationT);
        }
    }

    public void ShowThreat(Transform newThreat)
    {
        if (target == null || newThreat == null)
        {
            return;
        }

        threat = newThreat;
        viewMode = ViewMode.ThreatReveal;
        // 위치를 즉시 바꾸지 않고 LateUpdate에서 위협 구도로 부드럽게 연결
    }

    public void BeginChaseView()
    {
        if (target == null)
        {
            return;
        }

        viewMode = ViewMode.ChaseSide; // 옆에서 플레이어와 바위를 함께 보여 줌
        // 옆 구도로 이동하는 동안에도 회전·위치 보간 유지
    }

    public void BeginTraversalView(Transform chaser, bool snap = false)
    {
        if (target == null) return;
        threat = chaser;
        viewMode = ViewMode.TraversalSide; // 낙하와 착지 이후에도 오른쪽 옆 구도 유지
        shakeRemaining = 0f;
        if (!snap) return; // 평소에는 LateUpdate가 부드럽게 넓혀 줌
        transform.position = target.position + GetTraversalOffset(); // 재시작의 검은 화면 안에서만 즉시 정렬
        transform.LookAt(GetLookPoint());
    }

    private Vector3 GetTraversalOffset()
    {
        if (threat == null) return traversalSideOffset;
        float separation = Vector3.Distance(target.position, threat.position);
        float extraDistance = Mathf.Min(separation * 0.35f, 6f); // 거리가 벌어져도 무한히 멀어지지 않음
        return traversalSideOffset + Vector3.right * extraDistance;
    }

    public void PlayImpactShake(float duration, float strength)
    {
        shakeRemaining = Mathf.Max(0f, duration); // 음수 입력 방지
        shakeStrength = Mathf.Max(0f, strength);
    }

    public void SnapToTarget()
    {
        if (target == null)
        {
            return;
        }

        threat = null;
        viewMode = ViewMode.Follow; // 재시작할 때는 처음 구도로
        shakeRemaining = 0f; // 이전 충격 흔들림을 재시작 화면에 남기지 않음
        ResetOrbitAngles(); // 암전 뒤에는 기본 탐험 방향으로 복구
        transform.position = ResolveCameraPosition(target.position + GetFollowOffset(0f));
        transform.LookAt(target.position + lookOffset);
    }

    private Vector3 GetLookPoint()
    {
        if (viewMode == ViewMode.TraversalSide)
        {
            Vector3 point = target.position + lookOffset + Vector3.forward * 2f; // 다음 발판 쪽 시야 확보
            if (threat != null)
            {
                Vector3 separation = threat.position - target.position;
                point += Vector3.ClampMagnitude(separation, 10f) * 0.2f; // 추격자는 함께 보되 플레이어를 놓치지 않음
            }
            return point;
        }

        if (viewMode == ViewMode.ThreatReveal && threat != null)
        {
            return threat.position + lookOffset; // 충격 직후 바위를 정면으로
        }

        if (viewMode == ViewMode.ChaseSide && threat != null)
        {
            return Vector3.Lerp(target.position, threat.position, 0.15f) + lookOffset; // 둘 다 프레임 안에
        }

        return target.position + lookOffset;
    }

    public void ReturnToFollow()
    {
        threat = null;
        viewMode = ViewMode.Follow; // LateUpdate에서 현재 위치부터 보간
        shakeRemaining = 0f;
        Vector3 relative = transform.position - (target != null ? target.position + lookOffset : transform.position);
        if (enableMouseOrbit && relative.sqrMagnitude > 0.0001f)
        {
            wantedYaw = yaw = Mathf.Atan2(-relative.x, -relative.z) * Mathf.Rad2Deg;
            wantedPitch = pitch = Mathf.Clamp(Mathf.Atan2(relative.y,
                new Vector2(relative.x, relative.z).magnitude) * Mathf.Rad2Deg,
                Mathf.Min(minPitch, maxPitch), Mathf.Max(minPitch, maxPitch));
        } // 현재 바라보던 방향부터 탐험 회전을 이어감
    }
}
