using UnityEngine;

[DisallowMultipleComponent]
public class TimedCollectionPickupVisual : MonoBehaviour
{
    [SerializeField] private Transform visualRoot; // 외형만 움직여 수집 Collider의 위치를 유지
    [SerializeField, Min(0f)] private float hoverHeight = 0.06f;
    [SerializeField, Min(0f)] private float hoverSpeed = 2.5f;
    [SerializeField] private float spinSpeed = 90f;
    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private float elapsed;

    private void Awake()
    {
        if (visualRoot == null) return;
        initialPosition = visualRoot.localPosition;
        initialRotation = visualRoot.localRotation;
    }

    private void Update()
    {
        if (visualRoot == null) return;
        elapsed += Time.deltaTime;
        visualRoot.localPosition = initialPosition + Vector3.up * (Mathf.Sin(elapsed * hoverSpeed) * hoverHeight);
        visualRoot.localRotation = initialRotation * Quaternion.Euler(0f, elapsed * spinSpeed, 0f);
    }

    private void OnDisable()
    {
        if (visualRoot == null) return;
        visualRoot.localPosition = initialPosition;
        visualRoot.localRotation = initialRotation;
        elapsed = 0f; // 재도전 때 기준 위치가 누적되지 않게 복원
    }
}