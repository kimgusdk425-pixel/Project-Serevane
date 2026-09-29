using UnityEngine;

public enum ItemInteractionResult
{
    None,
    ItemCollected,
    DoorOpened
}

// E 키로 상호작용할 수 있는 아이템 대상의 공통 부모입니다.
public abstract class ItemInteractionTarget : MonoBehaviour
{
    // 현재 인벤토리 상태에 맞는 안내 문구를 반환합니다.
    public abstract string GetInteractionPrompt(PlayerInventory inventory);

    // 성공한 동작만 결과로 돌려주고, 실패하면 None을 돌려줍니다.
    public abstract ItemInteractionResult Interact(PlayerInventory inventory);
}
