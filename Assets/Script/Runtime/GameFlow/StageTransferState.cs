using UnityEngine;

public static class StageTransferState
{
    private static string destination;
    private static int fragments;
    private static ItemType item;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void Clear()
    {
        destination = null; // 새 Play 실행에서는 이전 검사 기록을 사용하지 않음
        fragments = 0;
        item = ItemType.None;
    }

    public static void Capture(string sceneName, PlayerJumpProgress progress, PlayerInventory inventory)
    {
        destination = sceneName;
        fragments = progress != null ? progress.CollectedFragments : 0;
        item = inventory != null ? inventory.CurrentItem : ItemType.None;
    }

    public static bool TryConsume(string sceneName, out int count, out ItemType carriedItem)
    {
        count = fragments;
        carriedItem = item;
        bool matches = destination == sceneName;
        Clear(); // 한 번의 씬 전환에서만 사용
        return matches;
    }
}
