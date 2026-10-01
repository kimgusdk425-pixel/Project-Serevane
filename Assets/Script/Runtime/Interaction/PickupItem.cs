using UnityEngine;

public class PickupItem : ItemInteractionTarget
{
    [SerializeField] private ItemType itemType = ItemType.Key; // 주울 아이템 종류
    [SerializeField] private string itemName = "KEY"; // 안내 문구에 표시할 이름

    public override bool CanSelect(PlayerInventory inventory) =>
        base.CanSelect(inventory) && inventory != null &&
        inventory.CurrentItem == ItemType.None && itemType != ItemType.None; // 받을 수 없는 아이템은 후보에서 제외

    public override string GetInteractionPrompt(PlayerInventory inventory)
    {
        if (inventory == null || inventory.CurrentItem != ItemType.None)
        {
            return string.Empty; // 이미 다른 아이템을 들고 있으면 대상 숨김
        }

        return $"[ E ]  PICK UP {itemName}";
    }

    public override ItemInteractionResult Interact(PlayerInventory inventory)
    {
        if (inventory == null || !inventory.TryAddItem(itemType))
        {
            return ItemInteractionResult.None;
        }

        // Destroy하지 않고 비활성화하면 씬을 다시 시작했을 때 다시 나타납니다.
        gameObject.SetActive(false);
        return ItemInteractionResult.ItemCollected;
    }
}
