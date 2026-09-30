using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Vector3 offset = new Vector3(0f, 5f, -7f); // 추적 거리
    [SerializeField] private float smooth = 5f; // 추적 속도
    [SerializeField] private Vector3 lookOffset = new Vector3(0f, 1f, 0f); // 응시 높이
    [SerializeField] private Vector3 threatViewOffset = new Vector3(0f, 4f, 6f); // 뒤를 돌아보는 구도
    [SerializeField] private Vector3 chaseSideOffset = new Vector3(11f, 5.5f, -1.5f); // D가 화면 오른쪽인 횡스크롤 구도

    private Transform target; // 추적 대상
    private Transform threat; // 충격 순간 보여 줄 바위
    private ViewMode viewMode; // 평상시·충격·추격 카메라 구분
    private float shakeRemaining; // 충격 연출이 남은 시간
    private float shakeStrength; // 카메라 흔들림 크기

    private enum ViewMode
    {
        Follow,
        ThreatReveal,
        ChaseSide
    }

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

        Vector3 viewOffset = viewMode switch
        {
            ViewMode.ThreatReveal => threatViewOffset,
            ViewMode.ChaseSide => chaseSideOffset,
            _ => offset
        };
        Vector3 want = target.position + viewOffset; // 현재 구도의 위치
        float t = 1f - Mathf.Exp(-smooth * Time.deltaTime); // 프레임 독립 보간
        Vector3 nextPosition = Vector3.Lerp(transform.position, want, t); // 부드럽게 추적
        if (shakeRemaining > 0f)
        {
            shakeRemaining = Mathf.Max(0f, shakeRemaining - Time.deltaTime);
            nextPosition += Random.insideUnitSphere * shakeStrength; // 짧은 충격 흔들림
        }

        transform.position = nextPosition;
        transform.LookAt(GetLookPoint());
    }

    public void ShowThreat(Transform newThreat)
    {
        if (target == null || newThreat == null)
        {
            return;
        }

        threat = newThreat;
        viewMode = ViewMode.ThreatReveal;
        transform.position = target.position + threatViewOffset; // 조작 잠금 중 반대편으로 컷
        transform.LookAt(GetLookPoint());
    }

    public void BeginChaseView()
    {
        if (target == null)
        {
            return;
        }

        viewMode = ViewMode.ChaseSide; // 옆에서 플레이어와 바위를 함께 보여 줌
        transform.position = target.position + chaseSideOffset;
        transform.LookAt(GetLookPoint());
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
        transform.position = target.position + offset; // 재시작 때 카메라도 즉시 출발점으로
        transform.LookAt(target.position + lookOffset);
    }

    private Vector3 GetLookPoint()
    {
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
}
