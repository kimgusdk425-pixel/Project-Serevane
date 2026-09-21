using UnityEngine;

public class PressurePlate : MonoBehaviour
{
    [SerializeField] private SlidingDoor door; // 열 문
    [SerializeField] private bool latch = true; // true=A 유지. false=B 무게

    private bool opened; // 발동 기록

    private void OnValidate()
    {
        if (door == null) // 미연결 조기 발견
        {
            Debug.LogWarning($"{nameof(PressurePlate)}: Door가 비어 있음.", this);
        }

        Collider col = GetComponent<Collider>(); // 감지용 콜라이더
        if (col == null || !col.isTrigger) // Trigger 필수
        {
            Debug.LogWarning($"{nameof(PressurePlate)}: isTrigger 콜라이더 필요.", this);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (opened && latch) // 이미 열렸으면 무시
        {
            return;
        }

        if (other.GetComponentInParent<CarryableObject>() == null) // 상자만 반응
        {
            return;
        }

        opened = true; // 발동 기록
        if (door != null) // 미연결 방어
        {
            door.Open(); // 문 열기
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (latch) // A는 닫힘 없음
        {
            return;
        }

        if (other.GetComponentInParent<CarryableObject>() == null) // 상자만 반응
        {
            return;
        }

        opened = false; // 발동 해제
        if (door != null) // 미연결 방어
        {
            door.Close(); // B: 치우면 닫기
        }
    }
}
