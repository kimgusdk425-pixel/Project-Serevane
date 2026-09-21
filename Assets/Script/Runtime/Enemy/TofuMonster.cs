using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class TofuMonster : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 2f; // 추적 속도
    [SerializeField] private float catchDistance = 1f; // 접촉 판정 거리

    private CharacterController controller; // 충돌+이동 담당
    private Transform player; // 추적 대상
    private PlayerMovement playerMovement; // 리스폰 호출용

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
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
        controller.Move(movement * Time.deltaTime); // 벽 슬라이딩 이동

        if (Vector3.Distance(transform.position, player.position) <= catchDistance) // 접촉
        {
            playerMovement.Respawn(); // 시작으로 복귀
        }
    }
}
