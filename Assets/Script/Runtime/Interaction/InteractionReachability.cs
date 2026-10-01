using UnityEngine;

public static class InteractionReachability
{
    private static RaycastHit[] hits = new RaycastHit[16]; // 가득 찰 때만 확장해서 재사용

    public static bool CanReach(Transform player, Transform target, Collider targetCollider)
    {
        CharacterController body = player.GetComponent<CharacterController>();
        Vector3 origin = body != null ? body.bounds.center : player.position + Vector3.up;
        MeshCollider mesh = targetCollider as MeshCollider;
        Vector3 destination = mesh != null && !mesh.convex
            ? targetCollider.bounds.ClosestPoint(origin)
            : targetCollider.ClosestPoint(origin); // 큰 문도 원점이 아니라 가까운 표면을 검사
        return IsPathClear(origin, destination, player, target);
    }

    public static bool IsPathClear(Vector3 origin, Vector3 destination, Transform player, Transform target)
    {
        Vector3 offset = destination - origin;
        float distance = offset.magnitude;
        if (distance < 0.001f) return true; // 이미 대상 표면에 닿은 경우

        int count;
        do
        {
            count = Physics.RaycastNonAlloc(origin, offset / distance, hits, distance,
                ~0, QueryTriggerInteraction.Ignore);
            if (count < hits.Length) break;
            System.Array.Resize(ref hits, hits.Length * 2);
        } while (true);

        for (int i = 0; i < count; i++)
        {
            Transform hit = hits[i].collider.transform;
            if (hit.IsChildOf(player) || hit.IsChildOf(target)) continue; // 몸과 대상 자체는 장애물이 아님
            return false;
        }
        return true;
    }
}
