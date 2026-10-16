using UnityEngine;

[RequireComponent(typeof(Collider))]
public class TimedCollectionPickup : MonoBehaviour
{
    [SerializeField] private bool isStarter;
    [SerializeField] private Color pickupColor = new Color(1f, .75f, .12f, 1f);
    [SerializeField, Range(0f, 1f)] private float emissionStrength = .6f;
    private Renderer[] visuals;
    private Collider pickupCollider;
    private TimedCollectionChallenge challenge;
    private MaterialPropertyBlock colors;
    private bool originalTriggerEnabled, preview;
    private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorProperty = Shader.PropertyToID("_Color");
    private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
    public bool IsStarter => isStarter;
    public bool IsPreview => preview;

    private void Awake() => CacheComponents();
    private void OnEnable() => ApplyAppearance();

    private void CacheComponents()
    {
        if (visuals != null) return;
        visuals = GetComponentsInChildren<Renderer>(true);
        pickupCollider = GetComponent<Collider>();
        originalTriggerEnabled = pickupCollider.enabled;
        challenge = GetComponentInParent<TimedCollectionChallenge>();
        colors = new MaterialPropertyBlock();
    }

    public void SetPreview(bool value)
    {
        if (isStarter) return;
        preview = value;
        ApplyAppearance();
    }

    private void ApplyAppearance()
    {
        CacheComponents();
        pickupCollider.enabled = originalTriggerEnabled && !preview; // 미리보기는 길만 보여 주며 획득·시작 판정은 없음
        Color color = preview ? Color.Lerp(pickupColor, new Color(.32f, .39f, .43f, 1f), .6f) : pickupColor;
        foreach (var visual in visuals)
        {
            if (visual == null) continue;
            colors.Clear();
            visual.GetPropertyBlock(colors);
            colors.SetColor(BaseColor, color);
            colors.SetColor(ColorProperty, color);
            colors.SetColor(EmissionColor, color * emissionStrength * (preview ? .08f : 1f));
            visual.SetPropertyBlock(colors);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        var player = other.GetComponentInParent<PlayerMovement>();
        if (preview || player == null || !player.IsControlEnabled || challenge == null) return;
        if (isStarter) challenge.TryStart();
        else challenge.Collect(this);
    }
}
