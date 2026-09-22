using UnityEngine;
using UnityEngine.InputSystem; // WasPressedThisFrame용

public class PlayerCarryController : MonoBehaviour
{
    [SerializeField] private Transform holdPoint; // 손 위치
    [SerializeField] private float pickupRange = 2f; // 집기 범위
    [SerializeField] private float dropDistance = 1f; // 놓을 거리
    [SerializeField] private float placementPadding = 0.03f; // 접촉 오차 여유

    private InputSystem_Actions inputActions; // 자동 발급 입력표
    private CarryableObject held; // 든 물건. 없으면 null
    private Vector3 heldHalfExtents; // 놓기 검사에 쓸 물건 반크기
    private readonly Collider[] pickupHits = new Collider[16]; // 주변 검색용 재사용 배열

    public bool IsHolding => held != null; // UI가 현재 운반 상태를 확인
    public bool CanPickUpNearby => held == null && FindNearestCarryable() != null; // UI용 주변 상자 확인

    private void Awake()
    {
        inputActions = new InputSystem_Actions(); // 입력표 생성
        if (holdPoint == null) // 미지정 시 자동 생성
        {
            GameObject go = new GameObject("HoldPoint"); // 손 위치
            go.transform.SetParent(transform);
            go.transform.localPosition = new Vector3(0f, 1.4f, 0.6f); // 눈앞
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
        if (!inputActions.Player.Interact.WasPressedThisFrame()) // E 1회 아니면 무시
        {
            return;
        }

        if (held == null) // 빈손이면 집기
        {
            TryPickUp();
        }
        else // 들었으면 놓기
        {
            TryDrop();
        }
    }

    private void TryPickUp()
    {
        CarryableObject nearest = FindNearestCarryable(); // 주변에서 가장 가까운 상자
        if (nearest == null) // 범위 내 물건 없음
        {
            return;
        }

        held = nearest; // 추적 시작
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
    }
}
