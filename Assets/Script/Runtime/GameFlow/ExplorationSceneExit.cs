using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(BoxCollider))]
public class ExplorationSceneExit : MonoBehaviour
{
    [SerializeField] private string nextScene = "MainGame_Chase";
    [SerializeField] private CanvasGroup fadeOverlay;
    [SerializeField, Min(0f)] private float fadeSeconds = 0.4f;
    [SerializeField, Min(0)] private int minimumFragments; // 탐험 씬에서만 24개로 설정
    private bool loading;
    private PlayerMovement waitingPlayer;

    public int RequiredFragments => Mathf.Max(0, minimumFragments);

    public bool HasRequiredFragments(PlayerJumpProgress progress) => RequiredFragments == 0 ||
        (progress != null && progress.CollectedFragments >= RequiredFragments);

    private void OnTriggerEnter(Collider other) => TryEnter(other);
    private void OnTriggerStay(Collider other) => TryEnter(other);

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponentInParent<PlayerMovement>() == waitingPlayer) ClearNotice();
    }

    private void OnDisable()
    {
        if (!loading) ClearNotice();
    }

    public string GetWaitingNotice(PlayerMovement player)
    {
        if (waitingPlayer != player || waitingPlayer == null || loading) return null;
        if (!GetComponent<BoxCollider>().bounds.Intersects(player.GetComponent<CharacterController>().bounds)) return null; // 위치 복귀로 Trigger 종료 알림이 누락돼도 안내 해제
        PlayerCarryController carry = player.GetComponent<PlayerCarryController>();
        if (carry != null && carry.IsHolding) return "상자를 내려놓고 이동하세요";
        PlayerJumpProgress progress = player.GetComponent<PlayerJumpProgress>();
        if (HasRequiredFragments(progress)) return null;
        int count = progress != null ? progress.CollectedFragments : 0;
        return $"빛이 더 필요해요 · {count}/{RequiredFragments}";
    }

    private void TryEnter(Collider other)
    {
        if (loading) return;
        PlayerMovement player = other.GetComponentInParent<PlayerMovement>();
        if (player == null || !player.IsControlEnabled) return;
        PlayerCarryController carry = player.GetComponent<PlayerCarryController>();
        if ((carry != null && carry.IsHolding) || !HasRequiredFragments(player.GetComponent<PlayerJumpProgress>()))
        {
            waitingPlayer = player;
            return; // 필요한 성장량과 상자 소지 상태를 확인한 뒤 전환
        }
        ClearNotice();
        if (!Application.CanStreamedLevelBeLoaded(nextScene))
        {
            Debug.LogError($"ExplorationSceneExit: 빌드 목록에 {nextScene} 씬을 등록해야 합니다.", this);
            enabled = false;
            return;
        }
        loading = true;
        StartCoroutine(Transfer(player));
    }

    private void ClearNotice()
    {
        waitingPlayer = null;
    }

    private IEnumerator Transfer(PlayerMovement player)
    {
        player.SetControlEnabled(false);
        if (fadeOverlay != null)
        {
            float elapsed = 0f;
            while (elapsed < fadeSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                fadeOverlay.alpha = Mathf.Clamp01(elapsed / fadeSeconds);
                yield return null;
            }
            fadeOverlay.alpha = 1f;
        }
        StageTransferState.Capture(nextScene, player.GetComponent<PlayerJumpProgress>(), player.GetComponent<PlayerInventory>());
        AsyncOperation operation;
        try
        {
            operation = SceneManager.LoadSceneAsync(nextScene, LoadSceneMode.Single);
        }
        catch (System.Exception exception)
        {
            StageTransferState.Clear();
            if (fadeOverlay != null) fadeOverlay.alpha = 0f;
            player.SetControlEnabled(true);
            loading = false;
            Debug.LogException(exception, this);
            yield break;
        }
        yield return operation;
    }
}