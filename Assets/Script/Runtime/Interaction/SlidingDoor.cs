using UnityEngine;

public class SlidingDoor : MonoBehaviour
{
    [SerializeField] private float openHeight = 3f; // 열리는 높이
    [SerializeField] private float speed = 2f; // 이동 속도

    private Vector3 closedPos; // 닫힌 위치
    private float openAmount; // 0 닫힘. 1 열림

    private void Awake()
    {
        closedPos = transform.position; // 초기 위치 기억
    }

    public void Open()
    {
        openAmount = 1f; // 목표: 열림
    }

    public void Close() // B용. A 래치에서는 미사용
    {
        openAmount = 0f; // 목표: 닫힘
    }

    private void Update()
    {
        Vector3 target = closedPos + Vector3.up * (openHeight * openAmount); // 목표 위치
        transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime); // 등속 이동
    }
}
