using UnityEngine;

[DisallowMultipleComponent]
public class JumpFragmentGroupVisibility : MonoBehaviour
{
    [SerializeField] private PlayerJumpProgress progress;
    [SerializeField] private GameObject fragmentGroup; // 이 컴포넌트의 자식 그룹만 표시·숨김
    [SerializeField] private ExplorationSceneExit exitRequirement; // 출구 보충 조각만 필요 수량에 맞춰 숨김
    private JumpFragmentPickup[] pickups;

    public int AvailableFragments
    {
        get
        {
            CachePickups();
            int count = 0;
            foreach (JumpFragmentPickup pickup in pickups)
                if (pickup != null && pickup.gameObject.activeSelf) count++;
            return count;
        }
    }

    private void Awake() => CachePickups();

    private void OnEnable()
    {
        if (progress != null) progress.Changed += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        if (progress != null) progress.Changed -= Refresh;
    }

    private void CachePickups()
    {
        if (pickups != null) return;
        pickups = fragmentGroup != null
            ? fragmentGroup.GetComponentsInChildren<JumpFragmentPickup>(true)
            : new JumpFragmentPickup[0];
    }

    private void Refresh()
    {
        if (progress == null || fragmentGroup == null || fragmentGroup == gameObject) return;
        bool visible = exitRequirement == null || progress.CollectedFragments < exitRequirement.RequiredFragments;
        if (fragmentGroup.activeSelf != visible) fragmentGroup.SetActive(visible); // 먹은 자식은 다시 활성화하지 않음
    }
}