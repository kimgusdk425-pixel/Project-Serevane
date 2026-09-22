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

        if (player != null)
        {
            player.SetControlEnabled(false); // 중력은 유지하고 이동 입력만 멈추기

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
    }
}
