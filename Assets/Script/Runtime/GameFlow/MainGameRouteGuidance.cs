using TMPro;
using UnityEngine;

public class MainGameRouteGuidance : MonoBehaviour
{
    [SerializeField] private PlayerMovement player;
    [SerializeField] private TMP_Text label;
    [SerializeField] private bool useGrowthRouteGuidance;
    [SerializeField] private ExplorationSceneExit explorationExit;
    [SerializeField] private JumpFragmentGroupVisibility exitCatchUp;
    private PlayerJumpProgress progress;
    private string lastMessage;

    private void Awake()
    {
        if (player != null) progress = player.GetComponent<PlayerJumpProgress>();
    }

    private void Update()
    {
        if (player == null || label == null) return;
        float z = player.transform.position.z;
        string message = z < 10f ? "FIND THE LIGHT   |   WASD: MOVE   SPACE: JUMP" :
            z < 25f ? "MOVE THE BOX TO THE LEDGE   |   E: PICK UP / PUT DOWN" :
            z < 39f ? "PLACE THE SECOND BOX ON THE GOLD PLATE" :
            z < 53f ? "FIND THE KEY AND OPEN THE DOOR   |   E" :
            z < 276f ? GetValleyMessage(z) :
            z < 300f ? GetExitMessage() :
            z < 320f ? "FOLLOW THE PATH..." :
            z < 380f ? "ESCAPE   |   WHEN SIDE VIEW: D FORWARD / A BACK   W/S SIDESTEP" :
            z < 470f ? "KEEP RUNNING AND JUMP   |   D FORWARD / A BACK   W/S SIDESTEP   SPACE: JUMP" :
            "USE YOUR POWERED JUMP TO ESCAPE   |   D + SPACE";
        if (message == lastMessage && label.text == message) return;
        label.text = message;
        lastMessage = message; // 출구의 임시 안내가 끝나면 현재 수량 안내로 복원
    }

    private string GetExitMessage()
    {
        if (explorationExit == null) return "GATHER GOLD LIGHTS, THEN JUMP TO THE HIGH EXIT   |   SPACE: JUMP";
        if (explorationExit.HasRequiredFragments(progress)) return "EXIT READY   |   JUMP FROM THE LOW STONE TO THE EXIT   |   SPACE";
        int count = progress != null ? progress.CollectedFragments : 0;
        int needed = Mathf.Max(0, explorationExit.RequiredFragments - count);
        bool canCatchUp = exitCatchUp != null && exitCatchUp.AvailableFragments >= needed;
        string direction = canCatchUp ? "FOLLOW THE SMALL LIGHTS TO THE EXIT STONE" : "FOLLOW THE STONE TRAIL BACK TO EARLIER RUINS";
        return $"EXIT LIGHTS {count}/{explorationExit.RequiredFragments}   |   {direction}";
    }

    private string GetValleyMessage(float z)
    {
        if (!useGrowthRouteGuidance) return "EXPLORE THE OPEN VALLEY   |   GOLD LIGHTS INCREASE YOUR JUMP";
        int course = Mathf.Clamp(Mathf.FloorToInt((z - 70f) / 36f) + 1, 1, 6);
        return $"FOLLOW THE LIGHT   |   CLIMB RUIN {course}/6   |   GOLD LIGHTS POWER UP YOUR JUMP";
    }
}