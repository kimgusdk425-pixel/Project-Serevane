using UnityEngine;

[RequireComponent(typeof(Collider))]
public class TimedCollectionPickup : MonoBehaviour
{
    [SerializeField] private bool isStarter; // 시작 아이템이면 켜고, 코인이면 끔
    [SerializeField] private Color pickupColor = new Color(1f, 0.75f, 0.12f, 1f); // 임시 오브젝트 색

    public bool IsStarter => isStarter;

    private void OnEnable()
    {
        Renderer visual = GetComponent<Renderer>();
        if (visual == null)
        {
            return;
        }

        // 공유 머티리얼을 바꾸지 않고 이 오브젝트의 색만 지정합니다.
        MaterialPropertyBlock colors = new MaterialPropertyBlock();
        visual.GetPropertyBlock(colors);
        colors.SetColor("_BaseColor", pickupColor);
        colors.SetColor("_Color", pickupColor);
        visual.SetPropertyBlock(colors);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<PlayerMovement>() == null)
        {
            return; // 상자나 몬스터가 닿아도 수집하지 않음
        }

        TimedCollectionChallenge challenge = GetComponentInParent<TimedCollectionChallenge>();
        if (challenge == null)
        {
            return;
        }

        if (isStarter)
        {
            challenge.TryStart();
        }
        else
        {
            challenge.Collect(this);
        }
    }
}
