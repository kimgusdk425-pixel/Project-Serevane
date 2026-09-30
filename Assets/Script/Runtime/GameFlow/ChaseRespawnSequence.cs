using System.Collections;
using UnityEngine;

public class ChaseRespawnSequence : MonoBehaviour
{
    [SerializeField] private PlayerMovement player; // 재시작할 플레이어
    [SerializeField] private TofuMonster monster; // 재시작할 추격자
    [SerializeField] private CameraFollow cameraFollow; // 카메라 위치 동기화
    [SerializeField] private CanvasGroup fadeOverlay; // 화면을 덮는 검은 UI
    [SerializeField] private ChaseStartSequence startSequence; // 재도전 때 낙하 연출도 복원
    [SerializeField, Min(0f)] private float fadeOutSeconds = 0.45f;
    [SerializeField, Min(0f)] private float blackHoldSeconds = 0.25f;
    [SerializeField, Min(0f)] private float fadeInSeconds = 0.8f;

    private bool isRestarting; // 한 번 잡힐 때 한 번만 실행

    private void Awake()
    {
        if (fadeOverlay != null)
        {
            fadeOverlay.alpha = 0f; // 시작 화면은 밝게
            fadeOverlay.blocksRaycasts = false;
        }
    }

    public bool TryBeginCatch()
    {
        if (isRestarting)
        {
            return true; // 이미 연출 중이면 중복 실행 방지
        }

        if (player == null || monster == null || cameraFollow == null || fadeOverlay == null)
        {
            return false; // 연결이 비어 있으면 기존 즉시 재시작 방식 사용
        }

        StartCoroutine(RestartAfterCatch());
        return true;
    }

    private IEnumerator RestartAfterCatch()
    {
        isRestarting = true;
        player.SetControlEnabled(false); // 화면 전환 중 조작 중지
        monster.enabled = false; // 다시 잡는 동작 잠시 중지

        yield return Fade(0f, 1f, fadeOutSeconds); // 화면을 검게
        yield return new WaitForSecondsRealtime(blackHoldSeconds); // 짧은 숨 고르기

        player.Respawn(); // 어두운 동안 위치 복원
        player.SetSideScrollControls(false); // 재도전 시작은 원래 W 전진 조작
        monster.ResetToSpawn();
        if (startSequence != null)
        {
            startSequence.ResetForRetry(); // 돌을 올리고 추격자를 다시 대기시킴
        }
        cameraFollow.SnapToTarget(); // 이전 위치에서 카메라가 날아오지 않게
        yield return null; // 새 출발점 화면이 그려질 시간

        yield return Fade(1f, 0f, fadeInSeconds); // 새 출발점을 서서히 보여 주기
        player.SetControlEnabled(true);
        if (startSequence == null)
        {
            monster.enabled = true; // 시작 연출이 없는 기존 씬은 바로 추격
        }
        isRestarting = false;
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        if (duration <= 0f)
        {
            fadeOverlay.alpha = to;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime; // 일시정지의 영향을 받지 않음
            fadeOverlay.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        fadeOverlay.alpha = to;
    }
}
