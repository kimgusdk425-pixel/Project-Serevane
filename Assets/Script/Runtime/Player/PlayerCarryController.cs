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
    [SerializeField] private Transform holdPoint; // 손 위치
    [SerializeField] private Vector3 holdOffset = new Vector3(0f, 0.2f, 1.2f); // 몸을 가리지 않도록 상자를 조금 더 앞에 들기
    [SerializeField] private float pickupRange = 2f; // 집기 범위
    [SerializeField] private float dropDistance = 1f; // 놓을 거리
    [SerializeField] private float placementPadding = 0.03f; // 접촉 오차 여유

    private InputSystem_Actions inputActions; // 자동 발급 입력표
    private CarryableObject held; // 든 물건. 없으면 null
    private Vector3 heldHalfExtents; // 놓기 검사에 쓸 물건 반크기
    private readonly Collider[] pickupHits = new Collider[16]; // 주변 검색용 재사용 배열
    private bool directInputEnabled = true; // 다른 상호작용 통합기가 E 키를 맡는지 여부

    public bool IsHolding => held != null; // UI가 현재 운반 상태를 확인
    public bool CanPickUpNearby => held == null && FindNearestCarryable() != null; // UI용 주변 상자 확인

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
        Vector3 pos = transform.position + transform.forward * dropDistance; // 앞 위치
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
            if (overlap.transform.IsChildOf(transform)) // 플레이어 몸은 제외
            {
                continue;
            }

            CarryableObject carryable = overlap.GetComponentInParent<CarryableObject>();
            if (carryable == held) // 현재 들고 있는 물건은 제외
            {
                continue;
            }

            return false; // 벽이나 다른 물체가 차지한 자리
        }

        return true;
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
