using UnityEngine;

public class LockedDoor : ItemInteractionTarget
{
    [SerializeField] private ItemType requiredItem = ItemType.Key; // 문을 여는 아이템
    [SerializeField] private float openHeight = 3f; // 위로 올라갈 높이
    [SerializeField] private float openSpeed = 2f; // 문 이동 속도

    private Vector3 closedPosition;
    private bool isOpen;

    private void Awake()
    {
        closedPosition = transform.position; // 시작 위치를 닫힌 위치로 기억
    }

    public override string GetInteractionPrompt(PlayerInventory inventory)
    {
        if (isOpen)
        {
            return string.Empty;
        }

        if (inventory != null && inventory.HasItem(requiredItem))
        {
            return "[ E ]  OPEN DOOR";
        }

        return "[ E ]  NEED KEY";
    }

    public override ItemInteractionResult Interact(PlayerInventory inventory)
    {
        if (isOpen || inventory == null || !inventory.HasItem(requiredItem))
        {
            return ItemInteractionResult.None;
        }

        // 열쇠가 실제로 있는지 다시 확인한 뒤 소비합니다.
        // 이 시점까지 오지 않으면 열쇠는 절대 사라지지 않습니다.
        if (!inventory.RemoveItem(requiredItem))
        {
            return ItemInteractionResult.None;
        }

        isOpen = true; // 열림 상태로 바뀌는 순간에만 열쇠 소비
        return ItemInteractionResult.DoorOpened;
    }

    private void Update()
    {
        Vector3 target = closedPosition + Vector3.up * (isOpen ? openHeight : 0f);
        transform.position = Vector3.MoveTowards(
            transform.position,
            target,
            openSpeed * Time.deltaTime);
    }
}
