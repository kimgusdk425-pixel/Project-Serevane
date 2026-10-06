using UnityEngine;

public class SceneFlowController : MonoBehaviour
{
    [SerializeField] private GameObject demoEndPanel; // 데모 종료 안내 화면

    private bool isDemoComplete; // 종료 처리를 한 번만 실행

    private void Awake()
    {
        if (demoEndPanel != null)
        {
            demoEndPanel.SetActive(false); // 시작할 때 종료 화면 숨기기
        }
    }

    public void CompleteDemo(PlayerMovement player)
    {
        if (isDemoComplete)
        {
            return;
        }

        isDemoComplete = true;
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            CameraFollow cameraFollow = mainCamera.GetComponent<CameraFollow>();
            if (cameraFollow != null) cameraFollow.ReleaseCursorForUI(); // 종료 화면 버튼은 마우스로 누를 수 있게
        }

        if (player != null)
        {
            player.SetControlEnabled(false); // 중력은 유지하고 이동 입력만 멈추기

            PlayerItemInteraction interaction = player.GetComponent<PlayerItemInteraction>();
            if (interaction != null)
            {
                interaction.enabled = false; // 실제 E 입력 담당도 종료 시 함께 잠금
            }

            PlayerCarryController carry = player.GetComponent<PlayerCarryController>();
            if (carry != null)
            {
                carry.enabled = false; // 종료 후 상호작용도 멈추기
            }
        }

        if (demoEndPanel != null)
        {
            demoEndPanel.SetActive(true); // 종료 문구 표시
        }

        TofuMonster[] monsters = FindObjectsByType<TofuMonster>(FindObjectsSortMode.None);
        foreach (TofuMonster monster in monsters)
        {
            monster.enabled = false; // 성공 후 다시 잡히지 않도록 추격 중지
        }
    }
}
