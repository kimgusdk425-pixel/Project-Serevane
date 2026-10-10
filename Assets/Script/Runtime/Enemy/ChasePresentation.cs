using UnityEngine;

[RequireComponent(typeof(TofuMonster), typeof(CharacterController))]
public class ChasePresentation : MonoBehaviour
{
    [SerializeField] private MonsterTraversalRoute route;
    [SerializeField] private PlayerMovement player;
    [SerializeField] private CameraFollow cameraFollow;
    [SerializeField] private ParticleSystem impactDust;
    [SerializeField] private ParticleSystem stoneChips;
    [SerializeField] private ParticleSystem rollingDust;
    [SerializeField, Min(0f)] private float shakeRange = 12f;
    [SerializeField, Min(0f)] private float landingShake = 0.065f;
    private TofuMonster monster;
    private CharacterController body, playerBody;
    private Vector3 previousPosition;
    private float nextShake;
    private bool ending;

    private void Awake()
    {
        monster = GetComponent<TofuMonster>();
        body = GetComponent<CharacterController>();
        if (player != null) playerBody = player.GetComponent<CharacterController>();
    }

    private void OnEnable()
    {
        if (route != null)
        {
            route.Landed += OnLanded;
            route.FinalBarrierReached += OnFinalBarrier;
        }
        ResetPresentation();
    }

    private void OnDisable()
    {
        if (route != null)
        {
            route.Landed -= OnLanded;
            route.FinalBarrierReached -= OnFinalBarrier;
        }
        ClearParticles();
    }

    private void LateUpdate()
    {
        Vector3 displacement = transform.position - previousPosition;
        displacement.y = 0f;
        previousPosition = transform.position;
        if (rollingDust == null) return;
        rollingDust.transform.position = Feet();
        var emission = rollingDust.emission;
        emission.enabled = !ending && monster.isActiveAndEnabled && body.enabled && body.isGrounded &&
            displacement.sqrMagnitude > 0.000001f;
        if (emission.enabled && !rollingDust.isPlaying) rollingDust.Play();
    }

    public void PlayOpeningImpact()
    {
        if (!ending) EmitImpact(Feet(), true); // 시작 흔들림은 기존 시퀀스가 담당
    }

    private void OnLanded(Vector3 position, float fallSpeed)
    {
        if (ending) return;
        EmitImpact(position, fallSpeed > 10f);
        ShakeNearPlayer(position, landingShake);
    }

    private void OnFinalBarrier(Vector3 position)
    {
        if (ending) return;
        EmitImpact(position, true);
        ShakeNearPlayer(position, landingShake * 1.4f);
    }

    private Vector3 Feet() => body.bounds.center + Vector3.down * body.bounds.extents.y + Vector3.up * 0.08f;

    private void EmitImpact(Vector3 position, bool major)
    {
        if (impactDust != null)
        {
            impactDust.transform.position = position;
            var main = impactDust.main;
            main.startColor = position.y < -5f ? new Color(.45f, .51f, .56f, .55f) : new Color(.66f, .52f, .35f, .55f);
            if (!impactDust.isPlaying) impactDust.Play();
            impactDust.Emit(major ? 36 : 18);
        }
        if (stoneChips != null)
        {
            stoneChips.transform.position = position;
            if (!stoneChips.isPlaying) stoneChips.Play();
            stoneChips.Emit(major ? 12 : 6);
        }
    }

    private void ShakeNearPlayer(Vector3 position, float strength)
    {
        if (player == null || cameraFollow == null || !player.IsControlEnabled || Time.time < nextShake) return;
        float distance = Vector3.Distance(position, player.transform.position);
        float amount = 1f - Mathf.Clamp01(distance / Mathf.Max(.01f, shakeRange));
        if (amount <= 0f) return;
        if (playerBody != null && !playerBody.isGrounded) amount *= .3f; // 점프 중에는 착지 지점이 흔들리지 않게
        cameraFollow.PlayImpactShake(.12f, strength * amount);
        nextShake = Time.time + .35f;
    }

    public void ResetPresentation()
    {
        ending = false;
        nextShake = 0f;
        previousPosition = transform.position;
        ClearParticles();
    }

    public void BeginEnding()
    {
        ending = true;
        ClearParticles();
    }

    private void ClearParticles()
    {
        foreach (ParticleSystem effect in new[] { impactDust, stoneChips, rollingDust })
            if (effect != null) effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }
}