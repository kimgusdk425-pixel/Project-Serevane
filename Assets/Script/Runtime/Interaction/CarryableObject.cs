using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CarryableObject : MonoBehaviour
{
    public bool IsHeld { get; private set; } // 든 상태

    private Rigidbody rb; // 물리 담당
    private Collider[] colliders; // 충돌 담당들

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true; // 발판용. 넘어짐 방지
        colliders = GetComponentsInChildren<Collider>();
    }

    public void PickUp()
    {
        IsHeld = true; // 상태 전환
        rb.isKinematic = true; // 물리 끄기. 손을 따라다님
        SetColliders(false); // 플레이어와 충돌 방지
    }

    public void Drop()
    {
        IsHeld = false; // 상태 전환
        rb.isKinematic = false; // 물리 켜기. 떨어지고 밟힘
        SetColliders(true); // 발판 충돌 복구
    }

    public Vector3 GetPlacementHalfExtents()
    {
        Bounds combinedBounds = default; // 모든 충돌체를 감쌀 범위
        bool hasCollider = false;

        foreach (Collider c in colliders)
        {
            if (c == null || !c.enabled || c.isTrigger) // 실제 몸체만 계산
            {
                continue;
            }

            if (!hasCollider)
            {
                combinedBounds = c.bounds;
                hasCollider = true;
            }
            else
            {
                combinedBounds.Encapsulate(c.bounds);
            }
        }

        return hasCollider ? combinedBounds.extents : Vector3.one * 0.5f; // 충돌체 없을 때 예비 크기
    }

    private void SetColliders(bool enabled)
    {
        foreach (Collider c in colliders) // 자식 포함 전부
        {
            c.enabled = enabled;
        }
    }
}
