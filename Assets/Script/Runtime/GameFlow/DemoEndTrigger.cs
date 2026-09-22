using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class DemoEndTrigger : MonoBehaviour
{
    [SerializeField] private SceneFlowController sceneFlow; // 종료 연출 담당

    private void Reset()
    {
        BoxCollider trigger = GetComponent<BoxCollider>();
        trigger.isTrigger = true; // 플레이어를 막지 않는 도착 지점
    }

    private void Awake()
    {
        if (sceneFlow == null)
        {
            sceneFlow = FindFirstObjectByType<SceneFlowController>(); // 미연결 시 자동 탐색
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerMovement player = other.GetComponentInParent<PlayerMovement>();
        if (player == null || sceneFlow == null)
        {
            return;
        }

        sceneFlow.CompleteDemo(player); // 플레이어가 도착하면 데모 종료
    }
}
