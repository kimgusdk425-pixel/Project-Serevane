using TMPro;
using UnityEngine;

public class MainGameRouteGuidance : MonoBehaviour
{
    [SerializeField] private PlayerMovement player;
    [SerializeField] private TMP_Text label, controlsLabel;
    [SerializeField] private GameObject objectivePanel, progressPanel, interactionPanel;
    [SerializeField] private bool useGrowthRouteGuidance;
    [SerializeField] private ExplorationSceneExit explorationExit;
    [SerializeField] private JumpFragmentGroupVisibility exitCatchUp;
    [SerializeField] private Collider firstLedge;
    [SerializeField] private PressurePlate pressurePlate;
    [SerializeField] private LockedDoor keyDoor;
    [SerializeField] private GameObject[] fragmentGroups;
    [SerializeField] private TimedCollectionChallenge challenge;
    [SerializeField] private ChallengeFragmentReward challengeReward;
    [SerializeField] private MonsterTraversalRoute chaseRoute;
    private PlayerJumpProgress progress;
    private PlayerInventory inventory;
    private PlayerItemInteraction interaction;
    private CharacterController body;
    private JumpFragmentPickup[][] groups;
    private bool reachedFirstLedge;

    private void Awake()
    {
        if (player == null) return;
        progress = player.GetComponent<PlayerJumpProgress>();
        inventory = player.GetComponent<PlayerInventory>();
        interaction = player.GetComponent<PlayerItemInteraction>();
        body = player.GetComponent<CharacterController>();
        groups = new JumpFragmentPickup[fragmentGroups != null ? fragmentGroups.Length : 0][];
        for (int i = 0; i < groups.Length; i++)
            groups[i] = fragmentGroups[i] != null ? fragmentGroups[i].GetComponentsInChildren<JumpFragmentPickup>(true) : new JumpFragmentPickup[0];
    }

    private void LateUpdate()
    {
        if (player == null || label == null) return;
        bool visible = player.IsControlEnabled;
        SetVisible(objectivePanel, visible);
        SetVisible(progressPanel, visible);
        SetVisible(interactionPanel, visible && interaction != null && !string.IsNullOrEmpty(interaction.CurrentPrompt));
        if (!visible) return;
        ObserveFirstLedge();
        bool trialPriority = challenge != null && (challenge.IsRunning ||
            (challenge.IsAwaitingRetry && challenge.IsNearStart(player)) ||
            (challenge.IsCompleted && challengeReward != null && challengeReward.HasAvailableReward &&
                (challenge.IsShowingResult ||
                 (player.transform.position - challengeReward.RewardPosition).sqrMagnitude <= 49f)));
        label.enabled = !trialPriority;
        if (challenge != null) challenge.StatusText.enabled = trialPriority;
        string controls = explorationExit != null ? "이동 WASD · 점프 Space · 메뉴 Esc" :
            player.UsesSideScrollControls ? "D 전진 · A 뒤로 · W/S 옆 이동 · Space 점프" : "이동 WASD · 점프 Space · 메뉴 Esc";
        if (trialPriority)
        {
            challenge.StatusText.gameObject.SetActive(true);
            controls = challenge.IsRunning ? "시간 안에 푸른 빛을 모두 모으세요" :
                challenge.IsAwaitingRetry ? "시작 빛에서 벗어났다가 다시 닿으세요" : "근처에 나타난 금빛 보상을 모으세요";
        }
        else
        {
            SetText(label, GetGoal());
            if (challenge != null && !challenge.IsCompleted && challenge.IsNearStart(player, 4.5f))
                controls = $"선택 도전 · 푸른 빛에 닿으면 시작 ({challenge.TimeLimit:0}초 / {challenge.TargetCount}개)";
        }
        SetText(controlsLabel, controls);
    }

    private void ObserveFirstLedge()
    {
        if (reachedFirstLedge || firstLedge == null || body == null || !body.isGrounded) return;
        if (Physics.Raycast(body.bounds.center, Vector3.down, out var hit, body.bounds.extents.y + .3f,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) && hit.collider == firstLedge && hit.normal.y > .5f)
            reachedFirstLedge = true; // 위치를 지나친 것과 실제 발판 착지를 구분
    }

    private string GetGoal()
    {
        if (explorationExit == null)
        {
            if (chaseRoute != null && chaseRoute.IsFollowing) return "발판을 건너 탈출하세요";
            return player.UsesSideScrollControls ? "바위를 피해 달리세요" : "앞길로 이동하세요";
        }
        bool plateDone = pressurePlate == null || pressurePlate.IsActivated;
        bool doorDone = keyDoor == null || keyDoor.IsOpen;
        if (!reachedFirstLedge && firstLedge != null && !plateDone && !doorDone)
            return "상자를 옮겨 앞쪽 발판을 오르세요";
        if (!plateDone && !doorDone) return "상자를 금색 압력판에 올리세요";
        if (!doorDone) return inventory != null && inventory.HasItem(ItemType.Key)
            ? "열쇠로 앞쪽 문을 여세요" : "열쇠를 찾아주세요";
        string notice = explorationExit.GetWaitingNotice(player);
        if (!string.IsNullOrEmpty(notice)) return notice;
        if (explorationExit.HasRequiredFragments(progress)) return "강화 점프로 높은 출구에 도착하세요";
        if (exitCatchUp != null && exitCatchUp.AvailableFragments > 0 &&
            (player.transform.position - explorationExit.transform.position).sqrMagnitude < 256f)
            return "출구 근처 작은 빛을 더 모으세요";
        for (int i = 0; groups != null && i < groups.Length; i++)
            foreach (var pickup in groups[i])
                if (pickup != null && !pickup.IsCollected)
                    return i == 0 ? "문 너머 빛 조각을 모으세요" : $"유적 {i}/6의 빛을 모으세요";
        return "빛 조각을 모아 출구를 준비하세요";
    }

    private static void SetVisible(GameObject panel, bool visible)
    {
        if (panel != null && panel.activeSelf != visible) panel.SetActive(visible);
    }

    private static void SetText(TMP_Text text, string value)
    {
        if (text != null && text.text != value) text.text = value;
    }
}
