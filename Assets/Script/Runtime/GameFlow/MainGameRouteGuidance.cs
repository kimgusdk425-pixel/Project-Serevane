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
    [SerializeField] private Transform objectiveCue, exitDestination;
    [SerializeField] private GameObject[] fragmentGroups;
    [SerializeField] private TimedCollectionChallenge challenge;
    [SerializeField] private ChallengeFragmentReward challengeReward;
    [SerializeField] private MonsterTraversalRoute chaseRoute;
    private TimedCollectionChallenge[] challenges;
    private ChallengeFragmentReward[] challengeRewards;
    private PlayerJumpProgress progress;
    private PlayerInventory inventory;
    private PlayerItemInteraction interaction;
    private CharacterController body;
    private JumpFragmentPickup[][] groups;
    private string[] groupMessages;
    private string cachedGroupGoal;
    private int shownGroup = -1, shownGroupCount = -1;
    private bool shownOptional;
    private bool reachedFirstLedge;

    private void Awake()
    {
        if (player == null) return;
        challenges = FindObjectsByType<TimedCollectionChallenge>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        challengeRewards = new ChallengeFragmentReward[challenges.Length];
        for (int i = 0; i < challenges.Length; i++)
            challengeRewards[i] = challenges[i].GetComponent<ChallengeFragmentReward>();
        progress = player.GetComponent<PlayerJumpProgress>();
        inventory = player.GetComponent<PlayerInventory>();
        interaction = player.GetComponent<PlayerItemInteraction>();
        body = player.GetComponent<CharacterController>();
        groups = new JumpFragmentPickup[fragmentGroups != null ? fragmentGroups.Length : 0][];
        groupMessages = new string[groups.Length];
        string[] routes = { "문 너머 빛 · 점프 준비", "유적 1/6 · 기본 점프", "유적 2/6 · 틈 건너기", "유적 3/6 · 연속 도약", "유적 4/6 · 방향 전환", "유적 5/6 · 높은 단차", "유적 6/6 · 마지막 등반" };
        for (int i = 0; i < groups.Length; i++)
        {
            groups[i] = fragmentGroups[i] != null ? fragmentGroups[i].GetComponentsInChildren<JumpFragmentPickup>(true) : new JumpFragmentPickup[0];
            groupMessages[i] = i < routes.Length ? routes[i] : $"유적 {i} · 빛 수집";
        }
    }

    private void LateUpdate()
    {
        if (player == null || label == null) return;
        bool visible = player.IsControlEnabled;
        SetVisible(progressPanel, visible && Application.isEditor);
        SetVisible(interactionPanel, visible && interaction != null && !string.IsNullOrEmpty(interaction.CurrentPrompt));
        if (!visible)
        {
            SetVisible(objectivePanel, false);
            SetObjectiveCue(false, Vector3.zero);
            return;
        }
        ObserveFirstLedge();
        SelectChallenge();
        bool trialPriority = challenge != null && (challenge.IsRunning ||
            (challenge.IsAwaitingRetry && challenge.IsNearStart(player)) ||
            (challenge.IsCompleted && challengeReward != null && challengeReward.HasAvailableReward &&
                (challenge.IsShowingResult ||
                 (player.transform.position - challengeReward.RewardPosition).sqrMagnitude <= 49f)));
        SetVisible(objectivePanel, Application.isEditor || trialPriority); // 빌드에서도 도전 타이머가 속한 패널은 필요할 때 표시
        label.enabled = Application.isEditor && !trialPriority;
        if (controlsLabel != null) controlsLabel.enabled = Application.isEditor;
        if (challenge != null) challenge.StatusText.enabled = trialPriority;
        string controls = explorationExit != null ? "이동 WASD · 점프 Space · 메뉴 Esc" :
            player.UsesSideScrollControls ? "D 전진 · A 뒤로 · W/S 옆 이동 · Space 점프" : "이동 WASD · 점프 Space · 메뉴 Esc";
        if (trialPriority)
        {
            challenge.StatusText.gameObject.SetActive(true);
            bool showReward = challenge.IsCompleted && challengeReward != null && challengeReward.HasAvailableReward;
            SetObjectiveCue(showReward, showReward ? challengeReward.RewardPosition : Vector3.zero);
            controls = challenge.IsRunning ? "시간 안에 푸른 빛을 모두 모으세요" :
                challenge.IsAwaitingRetry ? "다시 닿아 재도전 · 지나가면 유적 탐험" : "근처에 나타난 금빛 보상을 모으세요";
        }
        else
        {
            SetText(label, GetGoal(out Transform destination));
            SetObjectiveCue(destination != null, destination != null ? destination.position : Vector3.zero);
            if (challenge != null && !challenge.IsCompleted && challenge.IsNearStart(player, 4.5f))
                controls = "시간제 도전 시작";
        }
        SetText(controlsLabel, controls);
    }

    private void SelectChallenge()
    {
        int selected = -1;
        float nearest = float.PositiveInfinity;
        bool runningFound = false;
        for (int i = 0; challenges != null && i < challenges.Length; i++)
        {
            var candidate = challenges[i];
            if (candidate == null) continue;
            if (candidate.StatusText != null) candidate.StatusText.enabled = false;
            var reward = challengeRewards[i];
            bool availableReward = reward != null && reward.HasAvailableReward;
            if (candidate.IsCompleted && !availableReward && !candidate.IsShowingResult) continue;
            if (runningFound && !candidate.IsRunning) continue;
            Vector3 destination = availableReward ? reward.RewardPosition : candidate.transform.position;
            float distance = (player.transform.position - destination).sqrMagnitude;
            if (candidate.IsRunning && !runningFound)
            {
                runningFound = true;
                nearest = float.PositiveInfinity;
            }
            if (distance >= nearest) continue;
            nearest = distance;
            selected = i;
        }
        challenge = selected >= 0 ? challenges[selected] : null;
        challengeReward = selected >= 0 ? challengeRewards[selected] : null;
    }

    private void ObserveFirstLedge()
    {
        if (reachedFirstLedge || firstLedge == null || body == null || !body.isGrounded) return;
        if (Physics.Raycast(body.bounds.center, Vector3.down, out var hit, body.bounds.extents.y + .3f,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) && hit.collider == firstLedge && hit.normal.y > .5f)
            reachedFirstLedge = true; // 위치를 지나친 것과 실제 발판 착지를 구분
    }

    private string GetGoal(out Transform destination)
    {
        destination = null;
        if (explorationExit == null)
        {
            if (chaseRoute != null && chaseRoute.IsFollowing) return "발판을 건너 탈출하세요";
            return player.UsesSideScrollControls ? "바위를 피해 달리세요" : "앞길로 이동하세요";
        }
        bool plateDone = pressurePlate == null || pressurePlate.IsActivated;
        bool doorDone = keyDoor == null || keyDoor.IsOpen;
        if (!reachedFirstLedge && firstLedge != null && !plateDone && !doorDone)
        {
            return "상자를 옮겨 앞쪽 발판을 오르세요";
        }
        if (!plateDone && !doorDone)
        {
            return "상자를 금색 압력판에 올리세요";
        }
        if (!doorDone)
        {
            bool hasKey = inventory != null && inventory.HasItem(ItemType.Key);
            return hasKey ? "열쇠로 앞쪽 문을 여세요" : "열쇠를 찾아주세요";
        }
        string notice = explorationExit.GetWaitingNotice(player);
        if (!string.IsNullOrEmpty(notice))
        {
            destination = exitDestination != null ? exitDestination : explorationExit.transform;
            return notice;
        }
        bool exitReady = explorationExit.HasRequiredFragments(progress);
        int nextGroup = FindNextGroup(exitReady);
        bool nearExit = (player.transform.position - explorationExit.transform.position).sqrMagnitude <= 576f;
        if (exitReady && (nearExit || nextGroup < 0))
        {
            destination = exitDestination != null ? exitDestination : explorationExit.transform;
            return "강화 점프로 높은 출구에 도착하세요";
        }
        if (!exitReady && exitCatchUp != null && exitCatchUp.AvailableFragments > 0 &&
            (player.transform.position - explorationExit.transform.position).sqrMagnitude < 256f)
        {
            destination = exitCatchUp.GetComponentInChildren<JumpFragmentPickup>()?.transform;
            return "출구 근처 작은 빛을 더 모으세요";
        }
        if (nextGroup >= 0)
        {
            destination = fragmentGroups[nextGroup].transform;
            return GetGroupGoal(nextGroup, exitReady);
        }
        return "빛 조각을 모아 출구를 준비하세요";
    }

    private int FindNextGroup(bool optional)
    {
        for (int i = 0; groups != null && i < groups.Length; i++)
        {
            if (fragmentGroups[i] == null) continue;
            if (optional && fragmentGroups[i].transform.position.z < player.transform.position.z - 12f) continue; // 충분히 성장했다면 놓친 빛 때문에 되돌리지 않음
            foreach (var pickup in groups[i])
                if (pickup != null && !pickup.IsCollected && pickup.gameObject.activeInHierarchy) return i;
        }
        return -1;
    }

    private string GetGroupGoal(int index, bool optional)
    {
        int count = 0;
        foreach (var pickup in groups[index]) if (pickup != null && pickup.IsCollected) count++;
        if (cachedGroupGoal == null || shownGroup != index || shownGroupCount != count || shownOptional != optional)
        {
            shownGroup = index;
            shownGroupCount = count;
            shownOptional = optional;
            cachedGroupGoal = $"{(optional ? "선택 · " : "")}{groupMessages[index]} · {count}/{groups[index].Length}";
        }
        return cachedGroupGoal;
    }

    private void SetObjectiveCue(bool visible, Vector3 position)
    {
        if (objectiveCue == null) return;
        SetVisible(objectiveCue.gameObject, visible);
        if (visible) objectiveCue.position = position + Vector3.up * .9f; // 목적지 위에 한 표식만 표시
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
