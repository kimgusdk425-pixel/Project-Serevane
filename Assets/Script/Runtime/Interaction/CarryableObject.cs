using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CarryableObject : MonoBehaviour
{
    public bool IsHeld { get; private set; } // 든 상태

    private Rigidbody rb; // 물리 담당
    private Collider[] colliders; // 충돌 담당들
    private bool[] colliderStates; // 집기 직전 켜짐 여부

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true; // 발판용. 넘어짐 방지
        colliders = GetComponentsInChildren<Collider>(true);
    }

    public void PickUp()
    {
        if (IsHeld) return; // 중복 호출로 꺼진 상태를 원래 상태로 덮어쓰지 않음
        colliders = GetComponentsInChildren<Collider>(true);
        colliderStates = new bool[colliders.Length];
        for (int i = 0; i < colliders.Length; i++)
        {
            colliderStates[i] = colliders[i].enabled; // 원래 꺼진 예비 충돌체도 구분
            colliders[i].enabled = false;
        }
        IsHeld = true; // 상태 전환
        rb.isKinematic = true; // 물리 끄기. 손을 따라다님
    }

    public void Drop()
    {
        if (!IsHeld) return; // 들지 않은 물체의 충돌 상태는 바꾸지 않음
        IsHeld = false; // 상태 전환
        rb.isKinematic = false; // 물리 켜기. 떨어지고 밟힘
        rb.constraints |= RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionZ; // 앞뒤·좌우로 밀리지 않고 아래로만 떨어짐
        rb.linearVelocity = Vector3.zero; // 들고 이동하던 속도가 남지 않도록
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null) colliders[i].enabled = colliderStates[i]; // 원래 상태만 복원
        }
    }

    public Vector3 GetPlacementHalfExtents()
    {
        Bounds combinedBounds = default; // 모든 충돌체를 감쌀 범위
        bool hasCollider = false;

        foreach (Collider c in colliders)
        {
            if (c == null || !c.enabled || !c.gameObject.activeInHierarchy || c.isTrigger) // 실제 몸체만 계산
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

    public Bounds GetLocalCollisionBounds()
    {
        Bounds combined = default;
        bool hasPoint = false;
        foreach (Collider collider in colliders)
        {
            if (collider == null || !collider.enabled || !collider.gameObject.activeInHierarchy || collider.isTrigger) continue;
            BoxCollider box = collider as BoxCollider;
            Bounds worldBounds = collider.bounds;
            for (int corner = 0; corner < 8; corner++)
            {
                Vector3 signs = new Vector3((corner & 1) == 0 ? -1f : 1f,
                    (corner & 2) == 0 ? -1f : 1f, (corner & 4) == 0 ? -1f : 1f);
                Vector3 worldPoint = box != null
                    ? box.transform.TransformPoint(box.center + Vector3.Scale(box.size * 0.5f, signs))
                    : worldBounds.center + Vector3.Scale(worldBounds.extents, signs); // 비박스는 보수적으로 감쌈
                Vector3 localPoint = transform.InverseTransformPoint(worldPoint);
                if (!hasPoint)
                {
                    combined = new Bounds(localPoint, Vector3.zero);
                    hasPoint = true;
                }
                else combined.Encapsulate(localPoint);
            }
        }
        return hasPoint ? combined : new Bounds(Vector3.zero, Vector3.one); // 회전 전 물체 기준의 중심·크기
    }
}
