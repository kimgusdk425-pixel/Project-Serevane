using System.Collections;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class FallRespawnZone : MonoBehaviour
{
    [SerializeField] private CanvasGroup fadeOverlay;
    [SerializeField] private CameraFollow cameraFollow;
    private bool resetting;

    private void OnTriggerEnter(Collider other)
    {
        PlayerMovement player = other.GetComponentInParent<PlayerMovement>();
        if (resetting || player == null || fadeOverlay == null || cameraFollow == null) return;
        StartCoroutine(ReturnToCheckpoint(player));
    }

    private IEnumerator ReturnToCheckpoint(PlayerMovement player)
    {
        resetting = true;
        player.SetControlEnabled(false);
        yield return Fade(0f, 1f, 0.3f);
        player.Respawn(); // 조각과 이미 푼 퍼즐은 유지
        cameraFollow.SnapToTarget();
        yield return new WaitForSecondsRealtime(0.15f);
        yield return Fade(1f, 0f, 0.4f);
        player.SetControlEnabled(true);
        resetting = false;
    }

    private IEnumerator Fade(float from, float to, float seconds)
    {
        float elapsed = 0f;
        while (elapsed < seconds)
        {
            elapsed += Time.unscaledDeltaTime;
            fadeOverlay.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / seconds));
            yield return null;
        }
        fadeOverlay.alpha = to;
    }
}
