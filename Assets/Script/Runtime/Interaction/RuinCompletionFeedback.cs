using UnityEngine;

[DisallowMultipleComponent]
public class RuinCompletionFeedback : MonoBehaviour
{
    [SerializeField] private JumpFragmentPickup[] pickups;
    [SerializeField] private Renderer seal;
    private MaterialPropertyBlock original, working;
    private Color baseColor;
    private float pulseRemaining;
    private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
    public bool IsComplete { get; private set; }

    private void Awake()
    {
        original = new MaterialPropertyBlock();
        working = new MaterialPropertyBlock();
        if (seal == null) return;
        seal.GetPropertyBlock(original);
        seal.GetPropertyBlock(working);
        baseColor = seal.sharedMaterial.GetColor(BaseColor);
    }

    private void OnEnable()
    {
        if (pickups != null)
            foreach (var pickup in pickups)
                if (pickup != null) pickup.Collected += Refresh;
        Refresh();
        if (IsComplete) ApplyColor();
    }

    private void OnDisable()
    {
        if (pickups != null)
            foreach (var pickup in pickups)
                if (pickup != null) pickup.Collected -= Refresh;
        pulseRemaining = 0f;
        if (seal != null) seal.SetPropertyBlock(original);
    }

    private void Refresh()
    {
        if (IsComplete || pickups == null || pickups.Length == 0) return;
        foreach (var pickup in pickups)
            if (pickup == null || !pickup.IsCollected) return;
        IsComplete = true;
        pulseRemaining = .4f; // 실제 무리의 마지막 획득에서 한 번만 반응
    }

    private void LateUpdate()
    {
        if (pulseRemaining <= 0f) return;
        pulseRemaining = Mathf.Max(0f, pulseRemaining - Time.deltaTime);
        ApplyColor();
    }

    private void ApplyColor()
    {
        if (seal == null) return;
        float pulse = Mathf.Sin(Mathf.PI * (1f - pulseRemaining / .4f));
        Color complete = Color.Lerp(baseColor, new Color(.36f, .25f, .14f), .65f);
        working.SetColor(BaseColor, Color.Lerp(complete, new Color(1f, .82f, .43f), pulse * .8f));
        seal.SetPropertyBlock(working); // 완료 색만 남기며 성장·보상·소리는 추가하지 않음
    }
}
