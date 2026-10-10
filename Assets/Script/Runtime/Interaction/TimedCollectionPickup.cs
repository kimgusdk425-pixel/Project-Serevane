using UnityEngine;

[RequireComponent(typeof(Collider))]
public class TimedCollectionPickup : MonoBehaviour
{
    [SerializeField] private bool isStarter; // 시작 아이템이면 켜고, 코인이면 끔
    [SerializeField] private Color pickupColor = new Color(1f, 0.75f, 0.12f, 1f); // 시작 아이템과 미션 코인을 구분하는 색
    [SerializeField, Range(0f, 1f)] private float emissionStrength = 0.6f; // 색상과 별도로 발광 세기 조절

    public bool IsStarter => isStarter;

    private void OnEnable()
    {
        Renderer[] visuals = GetComponentsInChildren<Renderer>(true);
        if (visuals.Length == 0) return;
        MaterialPropertyBlock colors = new MaterialPropertyBlock();
        foreach (Renderer visual in visuals)
        {
            colors.Clear();
            visual.GetPropertyBlock(colors);
            colors.SetColor("_BaseColor", pickupColor);
            colors.SetColor("_Color", pickupColor);
            colors.SetColor("_EmissionColor", pickupColor * emissionStrength); // 공유 재질을 복제하지 않고 색과 발광 적용
            visual.SetPropertyBlock(colors);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<PlayerMovement>() == null) return;
        TimedCollectionChallenge challenge = GetComponentInParent<TimedCollectionChallenge>();
        if (challenge == null) return;
        if (isStarter) challenge.TryStart();
        else challenge.Collect(this);
    }
}