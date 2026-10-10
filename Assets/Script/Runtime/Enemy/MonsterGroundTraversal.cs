using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class MonsterGroundTraversal : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float jumpClearance = 0.45f;
    [SerializeField, Min(0.1f)] private float maximumJumpHeight = 1.8f;
    [SerializeField] private float gravity = -9.81f;
    private CharacterController body;
    private float verticalSpeed;
    private float originalStepOffset;

    private void Awake()
    {
        body = GetComponent<CharacterController>();
        originalStepOffset = body.stepOffset;
    }

    public void Tick(Vector3 horizontalVelocity, float deltaTime)
    {
        if (!body.enabled || deltaTime <= 0f) return;
        if (body.isGrounded && verticalSpeed <= 0f)
        {
            verticalSpeed = -2f;
            float obstacleHeight = FindObstacleHeight(horizontalVelocity.normalized);
            if (obstacleHeight > originalStepOffset + 0.05f && obstacleHeight + jumpClearance <= maximumJumpHeight)
                verticalSpeed = Mathf.Sqrt((obstacleHeight + jumpClearance) * -2f * gravity); // 지정된 바위만 자동 점프
        }
        verticalSpeed += gravity * deltaTime;
        body.stepOffset = body.isGrounded && verticalSpeed <= 0f ? originalStepOffset : 0f;
        body.Move((horizontalVelocity + Vector3.up * verticalSpeed) * deltaTime);
    }

    private float FindObstacleHeight(Vector3 direction)
    {
        if (direction.sqrMagnitude < 0.01f) return 0f;
        Bounds bounds = body.bounds;
        float radius = Mathf.Max(bounds.extents.x, bounds.extents.z);
        Vector3 side = Vector3.Cross(Vector3.up, direction);
        float height = 0f;
        for (int i = -1; i <= 1; i++)
        {
            Vector3 origin = new Vector3(bounds.center.x, bounds.min.y + 0.12f, bounds.center.z) + side * radius * i * 0.65f;
            if (!Physics.Raycast(origin, direction, out RaycastHit hit, radius + 1.1f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) continue;
            ChaseRunObstacle obstacle = hit.collider.GetComponent<ChaseRunObstacle>();
            if (obstacle == null || !obstacle.isActiveAndEnabled) continue;
            height = Mathf.Max(height, hit.collider.bounds.max.y - bounds.min.y);
        }
        return height;
    }

    public void ResetMotion()
    {
        verticalSpeed = 0f;
        if (body != null) body.stepOffset = originalStepOffset;
    }

    private void OnDisable() => ResetMotion();
}