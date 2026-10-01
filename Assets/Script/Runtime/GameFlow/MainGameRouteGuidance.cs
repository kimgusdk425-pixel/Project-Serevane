using TMPro;
using UnityEngine;

public class MainGameRouteGuidance : MonoBehaviour
{
    [SerializeField] private PlayerMovement player;
    [SerializeField] private TMP_Text label;
    private string lastMessage;

    private void Update()
    {
        if (player == null || label == null) return;
        float z = player.transform.position.z;
        string message = z < 10f ? "FIND THE LIGHT   |   WASD: MOVE   SPACE: JUMP" :
            z < 25f ? "MOVE THE BOX TO THE LEDGE   |   E: PICK UP / PUT DOWN" :
            z < 39f ? "PLACE THE SECOND BOX ON THE GOLD PLATE" :
            z < 53f ? "FIND THE KEY AND OPEN THE DOOR   |   E" :
            z < 300f ? "EXPLORE THE OPEN VALLEY   |   GOLD LIGHTS INCREASE YOUR JUMP" :
            z < 320f ? "FOLLOW THE PATH..." :
            z < 380f ? "ESCAPE   |   WHEN SIDE VIEW: D FORWARD / A BACK   W/S SIDESTEP" :
            "CLIMB THE FLOATING STEPS   |   WASD: MOVE   SPACE: JUMP";
        if (message == lastMessage) return;
        label.text = message;
        lastMessage = message; // 안내가 바뀔 때만 UI 갱신
    }
}
