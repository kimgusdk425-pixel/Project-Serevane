using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class RespawnCheckpoint : MonoBehaviour
{
    [SerializeField] private Transform returnPoint;
    [SerializeField] private MonsterTraversalRoute monsterRoute; // 추격 구간에서만 선택적으로 연결
    [SerializeField] private Transform monsterReturnPoint; // 플레이어보다 뒤쪽의 발판
    [SerializeField] private int monsterResumeStep; // 복귀 뒤 향할 경로 지점
    private bool activated;

    private void OnTriggerEnter(Collider other)
    {
        PlayerMovement player = other.GetComponentInParent<PlayerMovement>();
        if (activated || player == null || returnPoint == null) return;
        if (monsterRoute != null && !monsterRoute.RecordCheckpoint(monsterReturnPoint, monsterResumeStep))
        {
            Debug.LogWarning("RespawnCheckpoint: 추격자 복귀 연결을 확인해야 합니다.", this);
            return; // 두 복귀 지점 중 하나만 갱신하지 않음
        }
        player.SetRespawnPoint(returnPoint.position, returnPoint.rotation);
        activated = true; // 잡혀도 탐색 퍼즐을 다시 하지 않음
    }
}
