using System.Collections;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class FinalFallSequence : MonoBehaviour
{
    [SerializeField] private SceneFlowController sceneFlow;
    [SerializeField] private CanvasGroup fadeOverlay;
    [SerializeField] private TofuMonster monster;
    [SerializeField] private ChaseStartSequence chaseStart;
    [SerializeField] private ChaseRespawnSequence chaseRespawn;
    private bool finishing;

    private void OnTriggerEnter(Collider other)
    {
        PlayerMovement player = other.GetComponentInParent<PlayerMovement>();
        if (finishing || player == null || sceneFlow == null || fadeOverlay == null) return;
        finishing = true;
        if (monster != null) monster.enabled = false;
        if (chaseStart != null) { chaseStart.StopAllCoroutines(); chaseStart.enabled = false; }
        if (chaseRespawn != null) { chaseRespawn.StopAllCoroutines(); chaseRespawn.enabled = false; }
        StartCoroutine(Finish(player)); // 이전 연출이 종료 잠금을 다시 풀지 못하게 중단
    }

    private IEnumerator Finish(PlayerMovement player)
    {
        player.SetControlEnabled(false); // 실제 낙하는 유지
        yield return new WaitForSecondsRealtime(0.3f);
        float elapsed = 0f;
        while (elapsed < 0.8f)
        {
            elapsed += Time.unscaledDeltaTime;
            fadeOverlay.alpha = Mathf.Clamp01(elapsed / 0.8f);
            yield return null;
        }
        sceneFlow.CompleteDemo(player);
    }
}
