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
    [SerializeField] private bool useDeepLanding;
    [SerializeField] private Collider landingFloor;
    [SerializeField] private Transform landingReturnPoint;
    [SerializeField] private CameraFollow cameraFollow;
    [SerializeField] private FallRespawnZone fallRetry;
    [SerializeField] private Light sceneSun;
    [SerializeField] private Light endingLight;
    [SerializeField] private GameObject[] endingHud;
    [SerializeField, Min(0f)] private float landingPause = 1.2f;
    [SerializeField] private CanvasGroup endingText;
    [SerializeField, Min(0f)] private float departureSpeed = 3.6f;
    [SerializeField, Min(0.01f)] private float departureDuration = 2.2f;
    private bool finishing;
    public bool HasLanded { get; private set; }

    private void OnTriggerEnter(Collider other)
    {
        PlayerMovement player = other.GetComponentInParent<PlayerMovement>();
        if (finishing || player == null || sceneFlow == null || fadeOverlay == null) return;
        if (useDeepLanding && (landingFloor == null || landingReturnPoint == null || cameraFollow == null))
        {
            Debug.LogError("FinalFallSequence: 엔딩 착지 연결이 필요합니다.", this);
            return;
        }
        if (monster != null && monster.TryGetComponent<ChasePresentation>(out var presentation)) presentation.BeginEnding();
        finishing = true;
        if (monster != null) monster.enabled = false;
        if (chaseStart != null) { chaseStart.StopAllCoroutines(); chaseStart.enabled = false; }
        if (chaseRespawn != null) { chaseRespawn.StopAllCoroutines(); chaseRespawn.enabled = false; }
        StartCoroutine(Finish(player)); // 이전 연출이 종료 잠금을 다시 풀지 못하게 중단
    }

    private IEnumerator Finish(PlayerMovement player)
    {
        player.SetControlEnabled(false); // 실제 낙하는 유지
        if (useDeepLanding)
        {
            yield return FinishAfterLanding(player);
            yield break;
        }
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

    private IEnumerator FinishAfterLanding(PlayerMovement player)
    {
        if (fallRetry != null)
        {
            fallRetry.StopAllCoroutines();
            fallRetry.gameObject.SetActive(false); // 엔딩 낙하를 실패 판정으로 돌려보내지 않음
        }
        if (endingHud != null)
            foreach (GameObject hud in endingHud)
                if (hud != null) hud.SetActive(false);
        player.BeginScriptedDrift(Vector3.forward * departureSpeed, departureDuration); // 마지막 기둥에서 멀어지며 중력으로 낙하
        cameraFollow.BeginFinalFallView();
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            mainCamera.clearFlags = CameraClearFlags.SolidColor;
            mainCamera.backgroundColor = new Color(0.005f, 0.008f, 0.015f);
        }
        if (endingLight != null) endingLight.enabled = true;
        float sunStart = sceneSun != null ? sceneSun.intensity : 0f;
        Color ambientStart = RenderSettings.ambientLight;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.reflectionIntensity = 0.1f;
        fadeOverlay.alpha = 0f;
        CharacterController body = player.GetComponent<CharacterController>();
        float startHeight = player.transform.position.y;
        float elapsed = 0f;
        while (elapsed < 8f)
        {
            elapsed += Time.deltaTime;
            float depth = Mathf.InverseLerp(startHeight, landingFloor.bounds.max.y, player.transform.position.y);
            float darkness = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.1f, 0.85f, depth));
            if (sceneSun != null) sceneSun.intensity = Mathf.Lerp(sunStart, 0.09f, darkness);
            RenderSettings.ambientLight = Color.Lerp(ambientStart, new Color(0.10f, 0.14f, 0.20f), darkness);
            Bounds floor = landingFloor.bounds;
            bool overFloor = player.transform.position.x >= floor.min.x && player.transform.position.x <= floor.max.x &&
                player.transform.position.z >= floor.min.z && player.transform.position.z <= floor.max.z;
            if (elapsed > 0.3f && body.isGrounded && overFloor && body.bounds.min.y <= floor.max.y + 0.15f)
            {
                HasLanded = true;
                break;
            }
            yield return null;
        }
        if (!HasLanded)
        {
            Debug.LogWarning("FinalFallSequence: 착지가 지연되어 안전한 엔딩 위치로 복원합니다.", this);
            body.enabled = false;
            player.transform.SetPositionAndRotation(landingReturnPoint.position, landingReturnPoint.rotation);
            body.enabled = true;
            while (!body.isGrounded) yield return null;
            HasLanded = true;
        }
        if (sceneSun != null) sceneSun.intensity = 0.09f;
        RenderSettings.ambientLight = new Color(0.10f, 0.14f, 0.20f);
        cameraFollow.BeginFinalLandingView();
        yield return new WaitForSeconds(landingPause); // 착지 공간을 보여 준 뒤에만 종료 문구
        if (endingText != null) endingText.alpha = 0f;
        sceneFlow.CompleteDemo(player);
        if (endingText != null)
        {
            float textElapsed = 0f;
            while (textElapsed < 1.2f)
            {
                textElapsed += Time.unscaledDeltaTime;
                endingText.alpha = Mathf.SmoothStep(0f, 1f, textElapsed / 1.2f);
                yield return null;
            }
            endingText.alpha = 1f;
        }
    }
}
