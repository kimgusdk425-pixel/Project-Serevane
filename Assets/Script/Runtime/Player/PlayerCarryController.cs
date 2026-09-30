using UnityEngine;
using UnityEngine.InputSystem; // WasPressedThisFrame용

public enum CarryInteractionResult
{
    None,
    PickedUp,
    PutDown
}

public class PlayerCarryController : MonoBehaviour
{
    private const float DropClearance = 0.1f; // 놓을 때 플레이어 몸과 상자 사이 여유

    [SerializeField] private Transform holdPoint; // 손 위치
    [SerializeField] private Vector3 holdOffset = new Vector3(0f, 0.2f, 1.2f); // 몸을 가리지 않도록 상자를 조금 더 앞에 들기
    [SerializeField] private float pickupRange = 2f; // 집기 범위
    [SerializeField] private float dropDistance = 1f; // 놓을 거리
    [SerializeField] private float placementPadding = 0.03f; // 접촉 오차 여유

    private InputSystem_Actions inputActions; // 자동 발급 입력표
    private CharacterController playerCollider; // 놓을 때 플레이어 몸 크기 확인
    private CarryableObject held; // 든 물건. 없으면 null
    private Vector3 heldHalfExtents; // 놓기 검사에 쓸 물건 반크기
    private readonly Collider[] pickupHits = new Collider[16]; // 주변 검색용 재사용 배열
    private readonly Collider[] carryOverlapHits = new Collider[32]; // 든 상자가 차지할 자리 검사
    private readonly Collider[] carryCurrentHits = new Collider[32]; // 이미 닿은 벽에서 빠져나오는지 비교
    private bool directInputEnabled = true; // 다른 상호작용 통합기가 E 키를 맡는지 여부

    public bool IsHolding => held != null; // UI가 현재 운반 상태를 확인
    public bool CanPickUpNearby => held == null && FindNearestCarryable() != null; // UI용 주변 상자 확인

    public bool CanMoveHeldBox(Vector3 playerDisplacement, Quaternion nextRotation)
    {
        if (held == null)
        {
            return true;
        }

        Vector3 localHoldOffset = transform.InverseTransformPoint(holdPoint.position);
        Vector3 start = holdPoint.position;
        Vector3 end = transform.position + playerDisplacement + nextRotation * localHoldOffset;
        float stepLength = Mathf.Max(0.02f, Mathf.Min(0.1f, Mathf.Min(heldHalfExtents.x, heldHalfExtents.z)));
        int steps = Mathf.Max(1, Mathf.CeilToInt(Vector3.Distance(start, end) / stepLength));
        Vector3 previous = start;
        for (int i = 1; i <= steps; i++)
        {
            Vector3 next = Vector3.Lerp(start, end, i / (float)steps);
            if (!CanAdvanceCarryBox(previous, next))
            {
                return false; // 중간에 얇은 벽이 있어도 건너뛰지 않음
            }

            previous = next;
        }

        return true;
    }

    public void SetDirectInputEnabled(bool enabled)
    {
        directInputEnabled = enabled;
    }

    public float GetNearestCarryableDistance()
    {
        if (held != null)
        {
            return 0f; // 들고 있는 상자를 내려놓는 동작은 플레이어 바로 옆의 후보입니다.
        }

        CarryableObject nearest = FindNearestCarryable();
        if (nearest == null)
        {
            return float.MaxValue;
        }

        return (nearest.transform.position - transform.position).sqrMagnitude;
    }

    private void Awake()
    {
        inputActions = new InputSystem_Actions(); // 입력표 생성
        playerCollider = GetComponent<CharacterController>();
        if (holdPoint == null) // 미지정 시 자동 생성
        {
            GameObject go = new GameObject("HoldPoint"); // 손 위치
            go.transform.SetParent(transform, false); // 플레이어 회전을 그대로 따라가기
            go.transform.localPosition = holdOffset; // 쥐의 몸통 앞
            holdPoint = go.transform;
        }
    }

    private void OnEnable()
    {
        inputActions.Player.Enable(); // Player 맵 켜기
    }

    private void OnDisable()
    {
        inputActions.Player.Disable(); // Player 맵 끄기
    }

    private void OnDestroy()
    {
        inputActions.Dispose(); // 입력표 정리
    }

    private void Update()
    {
        if (!directInputEnabled)
        {
            return;
        }

        if (!inputActions.Player.Interact.WasPressedThisFrame()) // E 1회 아니면 무시
        {
            return;
        }

        TryInteract();
    }

    public CarryInteractionResult TryInteract()
    {
        if (held == null) // 빈손이면 집기
        {
            CarryableObject nearest = FindNearestCarryable();
            if (nearest == null)
            {
                return CarryInteractionResult.None;
            }

            Vector3 halfExtents = nearest.GetPlacementHalfExtents();
            if (!IsCarrySpaceFree(holdPoint.position, nearest, halfExtents))
            {
                return CarryInteractionResult.None; // 손 위치가 막혀 있으면 상자를 벽 속에 붙이지 않음
            }

            AttachToHoldPoint(nearest); // E를 누른 즉시 손에 붙이기
            return CarryInteractionResult.PickedUp;
        }

        TryDrop(); // 들었으면 놓기
        return held == null ? CarryInteractionResult.PutDown : CarryInteractionResult.None;
    }

    private void AttachToHoldPoint(CarryableObject target)
    {
        held = target; // 선택한 상자를 바로 손에 붙이기
        heldHalfExtents = held.GetPlacementHalfExtents(); // 충돌체를 끄기 전에 크기 기억
        held.PickUp(); // 물리 끄기
        held.transform.SetParent(holdPoint); // 손에 붙이기
        held.transform.localPosition = Vector3.zero; // 손 중앙
        held.transform.localRotation = Quaternion.identity; // 기울기 제거
    }

    private CarryableObject FindNearestCarryable()
    {
        int hitCount = Physics.OverlapSphereNonAlloc(
            transform.position,
            pickupRange,
            pickupHits,
            ~0,
            QueryTriggerInteraction.Ignore); // 배열을 재사용해 주변 Collider 검색

        CarryableObject nearest = null; // 가장 가까운 후보
        float best = float.MaxValue; // 최단 거리

        for (int i = 0; i < hitCount; i++)
        {
            Collider h = pickupHits[i];
            CarryableObject c = h.GetComponentInParent<CarryableObject>(); // 운반 가능?
            if (c == null || c.IsHeld) // 아니면 제외
            {
                continue;
            }

            float d = Vector3.Distance(transform.position, c.transform.position); // 거리 비교
            if (d < best)
            {
                best = d;
                nearest = c;
            }
        }

        return nearest;
    }

    private void TryDrop()
    {
        Vector3 forward = transform.forward;
        forward.y = 0f;
        forward.Normalize(); // 내려놓기는 바닥과 평행하게

        float boxReach = Mathf.Abs(forward.x) * heldHalfExtents.x
            + Mathf.Abs(forward.z) * heldHalfExtents.z; // 바라보는 방향의 상자 반너비
        float playerRadius = playerCollider != null ? playerCollider.radius : 0f;
        float safeDistance = playerRadius + boxReach + placementPadding + DropClearance;
        Vector3 pos = transform.position + forward * Mathf.Max(dropDistance, safeDistance); // 몸과 겹치지 않는 앞 위치
        pos.y = transform.position.y + 0.5f; // 살짝 위에서 낙하

        if (!CanPlaceAt(pos)) // 막힌 자리면 계속 들기
        {
            return;
        }

        held.transform.SetParent(null); // 손에서 떼기
        held.transform.SetPositionAndRotation(pos, Quaternion.identity); // 똑바로 놓기
        held.Drop(); // 물리 켜기
        held = null; // 추적 종료
    }

    private bool CanPlaceAt(Vector3 position)
    {
        Vector3 checkHalfExtents = new Vector3(
            Mathf.Max(0.01f, heldHalfExtents.x - placementPadding),
            Mathf.Max(0.01f, heldHalfExtents.y - placementPadding),
            Mathf.Max(0.01f, heldHalfExtents.z - placementPadding)); // 살짝 줄여 맞닿음 허용

        Collider[] overlaps = Physics.OverlapBox(
            position,
            checkHalfExtents,
            Quaternion.identity,
            ~0,
            QueryTriggerInteraction.Ignore); // 압력판 같은 Trigger는 통과

        foreach (Collider overlap in overlaps)
        {
            CarryableObject carryable = overlap.GetComponentInParent<CarryableObject>();
            if (carryable == held) // 현재 들고 있는 물건은 제외
            {
                continue;
            }

            return false; // 플레이어·벽·다른 물체가 차지한 자리
        }

        return true;
    }

    private bool IsCarrySpaceFree(Vector3 center, CarryableObject carriedBox, Vector3 halfExtents)
    {
        Vector3 checkSize = new Vector3(
            Mathf.Max(0.01f, halfExtents.x - placementPadding),
            Mathf.Max(0.01f, halfExtents.y - placementPadding),
            Mathf.Max(0.01f, halfExtents.z - placementPadding)); // 닿기만 한 면은 허용
        int hitCount = Physics.OverlapBoxNonAlloc(
            center, checkSize, carryOverlapHits, Quaternion.identity,
            ~0, QueryTriggerInteraction.Ignore);
        if (hitCount == carryOverlapHits.Length)
        {
            return false;
        }

        for (int i = 0; i < hitCount; i++)
        {
            if (IsCarryObstacle(carryOverlapHits[i], carriedBox))
            {
                return false;
            }
        }

        return true;
    }

    private bool CanAdvanceCarryBox(Vector3 from, Vector3 to)
    {
        Vector3 checkSize = new Vector3(
            Mathf.Max(0.01f, heldHalfExtents.x - placementPadding),
            Mathf.Max(0.01f, heldHalfExtents.y - placementPadding),
            Mathf.Max(0.01f, heldHalfExtents.z - placementPadding));
        int currentCount = Physics.OverlapBoxNonAlloc(
            from, checkSize, carryCurrentHits, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore);
        int nextCount = Physics.OverlapBoxNonAlloc(
            to, checkSize, carryOverlapHits, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore);
        if (currentCount == carryCurrentHits.Length || nextCount == carryOverlapHits.Length)
        {
            return false;
        }

        Bounds currentBox = new Bounds(from, checkSize * 2f);
        Bounds nextBox = new Bounds(to, checkSize * 2f);
        for (int i = 0; i < nextCount; i++)
        {
            Collider obstacle = carryOverlapHits[i];
            if (!IsCarryObstacle(obstacle, held))
            {
                continue;
            }

            bool touchedBefore = false;
            for (int j = 0; j < currentCount; j++)
            {
                if (carryCurrentHits[j] == obstacle)
                {
                    touchedBefore = true;
                    break;
                }
            }

            if (!touchedBefore)
            {
                return false; // 새 장애물 안으로 들어가려는 움직임
            }

            float oldOverlap = GetOverlapVolume(currentBox, obstacle.bounds);
            float newOverlap = GetOverlapVolume(nextBox, obstacle.bounds);
            if (newOverlap > oldOverlap + 0.0001f)
            {
                return false; // 이미 닿은 벽이라도 더 깊이 파고들면 차단
            }
        }

        return true; // 겹침이 같거나 줄어들면 벽에서 빠져나올 수 있음
    }

    private static float GetOverlapVolume(Bounds first, Bounds second)
    {
        float x = Mathf.Max(0f, Mathf.Min(first.max.x, second.max.x) - Mathf.Max(first.min.x, second.min.x));
        float y = Mathf.Max(0f, Mathf.Min(first.max.y, second.max.y) - Mathf.Max(first.min.y, second.min.y));
        float z = Mathf.Max(0f, Mathf.Min(first.max.z, second.max.z) - Mathf.Max(first.min.z, second.min.z));
        return x * y * z; // 벽과 상자가 겹친 양의 대략적인 비교값
    }

    private bool IsCarryObstacle(Collider other, CarryableObject carriedBox)
    {
        return other != null
            && !other.transform.IsChildOf(transform) // 플레이어 몸과 손은 제외
            && other.GetComponentInParent<CarryableObject>() != carriedBox; // 들고 있는 상자는 제외
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow; // 에디터 표시
        Gizmos.DrawWireSphere(transform.position, pickupRange); // 집기 범위
        Gizmos.color = Color.green;
        Vector3 preview = holdPoint != null ? holdPoint.position : transform.TransformPoint(holdOffset);
        Gizmos.DrawWireSphere(preview, 0.12f); // Inspector에서 손 위치 확인
    }
}
