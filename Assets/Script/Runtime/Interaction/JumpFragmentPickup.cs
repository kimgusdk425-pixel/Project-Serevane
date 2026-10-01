using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
[DisallowMultipleComponent]
public class JumpFragmentPickup : MonoBehaviour
{
    private bool collected; // 같은 접촉에서 중복 획득 방지

    private void Awake()
    {
        GetComponent<SphereCollider>().isTrigger = true; // 통과하며 줍는 감지 영역
        // 색과 발광은 Inspector의 머티리얼을 사용: 실행 중 파란색으로 덮어쓰지 않음
    }

    private void OnTriggerEnter(Collider other)
    {
        if (collected) return;
        PlayerJumpProgress progress = other.GetComponentInParent<PlayerJumpProgress>();
        if (progress == null) return; // 상자나 적은 수집할 수 없음
        collected = true; // 콜백이 겹쳐도 한 번만 처리
        if (!progress.TryCollectFragment())
        {
            collected = false;
            return; // 강화 실패 시 조각을 남김
        }
        gameObject.SetActive(false); // 성공했을 때만 조각 숨기기
    }
}
