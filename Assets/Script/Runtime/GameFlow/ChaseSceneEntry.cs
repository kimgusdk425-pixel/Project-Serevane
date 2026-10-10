using System.Collections;
using UnityEngine;

public class ChaseSceneEntry : MonoBehaviour
{
    [SerializeField] private PlayerMovement player;
    [SerializeField] private Transform returnPoint;
    [SerializeField] private CameraFollow cameraFollow;
    [SerializeField] private CanvasGroup fadeOverlay;
    [SerializeField, Min(0)] private int standaloneFragments = 20;
    [SerializeField, Min(0f)] private float fadeSeconds = 0.4f;

    private IEnumerator Start()
    {
        if (player == null || returnPoint == null) yield break;
        bool transferred = StageTransferState.TryConsume(gameObject.scene.name, out int count, out ItemType item);
        PlayerJumpProgress progress = player.GetComponent<PlayerJumpProgress>();
        PlayerInventory inventory = player.GetComponent<PlayerInventory>();
        if (progress != null) progress.RestoreFragments(transferred ? count : standaloneFragments);
        if (inventory != null) inventory.RestoreItem(transferred ? item : ItemType.None);
        player.SetRespawnPoint(returnPoint.position, returnPoint.rotation);
        player.SetSideScrollControls(false); // 추격 시작 연출이 끝나면 기존 코드가 D 전진으로 전환
        if (cameraFollow != null) cameraFollow.SnapToTarget();
        if (fadeOverlay == null || fadeSeconds <= 0f) yield break;
        player.SetControlEnabled(false);
        fadeOverlay.blocksRaycasts = false;
        float elapsed = 0f;
        while (elapsed < fadeSeconds)
        {
            elapsed += Time.unscaledDeltaTime;
            fadeOverlay.alpha = 1f - Mathf.Clamp01(elapsed / fadeSeconds);
            yield return null;
        }
        fadeOverlay.alpha = 0f;
        player.SetControlEnabled(true);
    }
}
