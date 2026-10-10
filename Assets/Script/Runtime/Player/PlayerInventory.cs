using System;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    // 이번 데모는 동시에 아이템 하나만 보관합니다.
    [SerializeField]
    private ItemType currentItem = ItemType.None;

    public ItemType CurrentItem => currentItem;

    // 아이템이 바뀌면 UI가 즉시 갱신할 수 있도록 알립니다.
    public event Action<ItemType> ItemChanged;

    public void RestoreItem(ItemType itemType)
    {
        currentItem = itemType;
        ItemChanged?.Invoke(currentItem);
    }

    public bool HasItem(ItemType itemType)
    {
        return currentItem == itemType;
    }

    public bool TryAddItem(ItemType itemType)
    {
        if (itemType == ItemType.None || currentItem != ItemType.None)
        {
            return false;
        }

        currentItem = itemType;
        ItemChanged?.Invoke(currentItem);
        return true;
    }

    // 문을 실제로 여는 순간처럼 아이템을 소비할 때 사용합니다.
    public bool RemoveItem(ItemType itemType)
    {
        if (!HasItem(itemType))
        {
            return false;
        }

        currentItem = ItemType.None;
        ItemChanged?.Invoke(currentItem);
        return true;
    }
}
