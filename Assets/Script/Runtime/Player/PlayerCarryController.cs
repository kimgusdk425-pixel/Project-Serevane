using UnityEngine;
using UnityEngine.InputSystem; // WasPressedThisFrame용

public class PlayerCarryController : MonoBehaviour
{
    [SerializeField] private Transform holdPoint; // 손 위치
    [SerializeField] private float pickupRange = 2f; // 집기 범위
    [SerializeField] private float dropDistance = 1f; // 놓을 거리

    private InputSystem_Actions inputActions; // 자동 발급 입력표
    private CarryableObject held; // 든 물건. 없으면 null

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
            Drop();
        }
    }

    private void TryPickUp()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, pickupRange); // 주변 스캔
        CarryableObject nearest = null; // 가장 가까운 후보
        float best = float.MaxValue; // 최단 거리

        foreach (Collider h in hits)
        {
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

        if (nearest == null) // 범위 내 물건 없음
        {
            return;
        }

        held = nearest; // 추적 시작
        held.PickUp(); // 물리 끄기
        held.transform.SetParent(holdPoint); // 손에 붙이기
        held.transform.localPosition = Vector3.zero; // 손 중앙
        held.transform.localRotation = Quaternion.identity; // 기울기 제거
    }

    private void Drop()
    {
        Vector3 pos = transform.position + transform.forward * dropDistance; // 앞 위치
        pos.y = transform.position.y + 0.5f; // 살짝 위에서 낙하

        held.transform.SetParent(null); // 손에서 떼기
        held.transform.SetPositionAndRotation(pos, Quaternion.identity); // 똑바로 놓기
        held.Drop(); // 물리 켜기
        held = null; // 추적 종료
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow; // 에디터 표시
        Gizmos.DrawWireSphere(transform.position, pickupRange); // 집기 범위
    }
}
