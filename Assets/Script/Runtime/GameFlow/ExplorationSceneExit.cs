using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(BoxCollider))]
public class ExplorationSceneExit : MonoBehaviour
{
    [SerializeField] private string nextScene = "MainGame_Chase";
    [SerializeField] private CanvasGroup fadeOverlay;
    [SerializeField] private TMP_Text guidanceLabel;
    [SerializeField, Min(0f)] private float fadeSeconds = 0.4f;
    [SerializeField, Min(0)] private int minimumFragments; // 탐험 씬에서만 24개로 설정
    private bool loading;
    private PlayerMovement waitingPlayer;
    private string previousGuidance;

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

    private void LateUpdate()
    {
        if (waitingPlayer == null || loading || guidanceLabel == null) return;
        PlayerCarryController carry = waitingPlayer.GetComponent<PlayerCarryController>();
        if (carry != null && carry.IsHolding)
            guidanceLabel.text = "PUT DOWN THE BOX TO CONTINUE   |   E";
        else
        {
            PlayerJumpProgress progress = waitingPlayer.GetComponent<PlayerJumpProgress>();
            int count = progress != null ? progress.CollectedFragments : 0;
            guidanceLabel.text = $"EXIT LIGHTS {count}/{RequiredFragments}   |   RETURN TO THE RUINS FOR MORE LIGHTS";
        }
    }

    private void TryEnter(Collider other)
    {
        if (loading) return;
        PlayerMovement player = other.GetComponentInParent<PlayerMovement>();
        if (player == null || !player.IsControlEnabled) return;
        PlayerCarryController carry = player.GetComponent<PlayerCarryController>();
        if ((carry != null && carry.IsHolding) || !HasRequiredFragments(player.GetComponent<PlayerJumpProgress>()))
        {
            if (waitingPlayer == null && guidanceLabel != null) previousGuidance = guidanceLabel.text;
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
        if (waitingPlayer != null && guidanceLabel != null) guidanceLabel.text = previousGuidance;
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