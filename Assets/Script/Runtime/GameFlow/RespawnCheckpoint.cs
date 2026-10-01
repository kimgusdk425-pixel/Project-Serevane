using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class RespawnCheckpoint : MonoBehaviour
{
    [SerializeField] private Transform returnPoint;
    private bool activated;

    private void OnTriggerEnter(Collider other)
    {
        PlayerMovement player = other.GetComponentInParent<PlayerMovement>();
        if (activated || player == null || returnPoint == null) return;
        player.SetRespawnPoint(returnPoint.position, returnPoint.rotation);
        activated = true; // 잡혀도 탐색 퍼즐을 다시 하지 않음
    }
}
