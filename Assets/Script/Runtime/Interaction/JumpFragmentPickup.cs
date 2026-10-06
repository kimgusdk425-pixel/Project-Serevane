using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
[DisallowMultipleComponent]
public class JumpFragmentPickup : MonoBehaviour
{
    [SerializeField, Min(0f)] private float attractionRadius = 1.8f; // 가까이 지나가도 놓치지 않는 자석 범위
    [SerializeField, Min(0.1f)] private float attractionSpeed = 5f;
    [SerializeField, Min(0.01f)] private float absorbDistance = 0.25f;
    [SerializeField, Min(0f)] private float floatAmplitude = 0.12f; // 자기 자리 주변에서만 잔잔하게 움직임
    [SerializeField, Min(0f)] private float floatSpeed = 1.2f;
    private PlayerJumpProgress player; // 한 명인 플레이어만 기억해 주변 장식 수에 영향받지 않음
    private PlayerJumpProgress target;
    private Vector3 homePosition;
    private float phase;
    private float nextSearch;
    private bool collected; // 같은 접촉에서 중복 획득 방지

    private void Awake()
    {
        GetComponent<SphereCollider>().isTrigger = true; // 통과하며 줍는 감지 영역
        homePosition = transform.position;
        phase = Random.Range(0f, Mathf.PI * 2f); // 같은 무리도 서로 다른 리듬
        nextSearch = Time.time + Random.Range(0f, 0.15f); // 탐색 시점을 분산
        // 색과 발광은 Inspector의 머티리얼을 사용: 실행 중 파란색으로 덮어쓰지 않음
    }

    private void Start()
    {
        player = FindFirstObjectByType<PlayerJumpProgress>(); // 씬 시작 또는 도전 보상 공개 때 한 번 연결
    }

    private void Update()
    {
        if (collected) return;
        if (target == null && Time.time >= nextSearch)
        {
            nextSearch = Time.time + 0.15f;
            if (player != null && CanAttract(player)) target = player; // 거리와 벽만 검사
        }

        if (target != null && !CanAttract(target)) target = null; // 벽 뒤나 재시작 위치까지 따라가지 않음
        if (target != null)
        {
            Vector3 destination = target.transform.position; // 플레이어 몸 중심으로 흡수
            transform.position = Vector3.MoveTowards(transform.position, destination, attractionSpeed * Time.deltaTime);
            if ((transform.position - destination).sqrMagnitude <= absorbDistance * absorbDistance) Collect(target);
        }
        else
        {
            Vector3 idle = homePosition + Vector3.up * Mathf.Sin(Time.time * floatSpeed + phase) * floatAmplitude;
            transform.position = Vector3.MoveTowards(transform.position, idle, attractionSpeed * Time.deltaTime);
        }
    }

    private bool CanAttract(PlayerJumpProgress progress)
    {
        if (!CanCollect(progress)) return false;
        Vector3 delta = progress.transform.position - transform.position;
        if (delta.sqrMagnitude > attractionRadius * attractionRadius) return false;
        if (Physics.Raycast(transform.position, delta.normalized, out RaycastHit hit, delta.magnitude,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            return hit.collider.GetComponentInParent<PlayerJumpProgress>() == progress; // 벽 너머 수집 방지
        return true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (collected) return;
        PlayerJumpProgress progress = other.GetComponentInParent<PlayerJumpProgress>();
        if (progress == null) return; // 상자나 적은 수집할 수 없음
        Collect(progress);
    }

    private void Collect(PlayerJumpProgress progress)
    {
        if (collected || !CanCollect(progress)) return; // 접촉과 자석 도착이 같은 조건을 사용
        collected = true; // 자석 도착과 접촉이 겹쳐도 한 번만 획득
        progress.TryCollectFragment(); // 최대 강화 이후에도 빛은 흡수하지만 수치는 늘지 않음
        gameObject.SetActive(false); // 최대 강화에서도 흡수한 빛은 숨기기
    }

    private bool CanCollect(PlayerJumpProgress progress)
    {
        if (progress == null || !progress.isActiveAndEnabled) return false;
        PlayerMovement movement = progress.GetComponent<PlayerMovement>();
        return movement != null && movement.IsControlEnabled; // 암전·낙하·종료 중에는 두 획득 방식 모두 잠금
    }
}
