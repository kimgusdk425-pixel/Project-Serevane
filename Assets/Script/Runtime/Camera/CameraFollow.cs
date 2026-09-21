using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Vector3 offset = new Vector3(0f, 5f, -7f); // 추적 거리
    [SerializeField] private float smooth = 5f; // 추적 속도
    [SerializeField] private Vector3 lookOffset = new Vector3(0f, 1f, 0f); // 응시 높이

    private Transform target; // 추적 대상

    private void Awake()
    {
        PlayerMovement p = FindFirstObjectByType<PlayerMovement>(); // 플레이어 자동 탐색
        if (p != null) // 찾았을 때만
        {
            target = p.transform;
        }
    }

    private void LateUpdate()
    {
        if (target == null) // 대상 없으면 정지
        {
            return;
        }

        Vector3 want = target.position + offset; // 목표 위치
        float t = 1f - Mathf.Exp(-smooth * Time.deltaTime); // 프레임 독립 보간
        transform.position = Vector3.Lerp(transform.position, want, t); // 부드럽게 추적
        transform.LookAt(target.position + lookOffset); // 플레이어 응시
    }
}
