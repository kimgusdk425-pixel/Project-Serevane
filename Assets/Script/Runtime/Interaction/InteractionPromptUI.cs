using TMPro;
using UnityEngine;

public class InteractionPromptUI : MonoBehaviour
{
    [SerializeField] private PlayerCarryController carryController; // 상호작용 상태 제공자
    [SerializeField] private TMP_Text promptText; // 화면에 표시할 TMP 글자

    private void Awake()
    {
        if (carryController == null)
        {
            carryController = FindFirstObjectByType<PlayerCarryController>(); // 미연결 시 플레이어 탐색
        }

        if (promptText == null)
        {
            promptText = GetComponent<TMP_Text>(); // 같은 오브젝트의 글자 사용
        }
    }

    private void Update()
    {
        if (promptText == null)
        {
            return;
        }

        string message = string.Empty;

        if (carryController != null)
        {
            if (carryController.IsHolding)
            {
                message = "[ E ]  PUT DOWN"; // 상자를 들고 있을 때
            }
            else if (carryController.CanPickUpNearby)
            {
                message = "[ E ]  PICK UP"; // 들 수 있는 상자가 가까울 때
            }
        }

        promptText.text = message;
        promptText.enabled = message.Length > 0; // 안내할 내용이 없으면 숨기기
    }
}
