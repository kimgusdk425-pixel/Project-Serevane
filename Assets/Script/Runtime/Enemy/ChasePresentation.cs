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
    [SerializeField] private GameplayFeedbackSettings settings;
    [SerializeField] private AudioSource rollingAudio, impactAudio;
    private float airborneSeconds, fallSpeed;
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
        StopAudio();
    }

    private void LateUpdate()
    {
        Vector3 displacement = transform.position - previousPosition;
        displacement.y = 0f;
        previousPosition = transform.position;
        bool active = !ending && monster.isActiveAndEnabled && body.enabled;
        float speed = Time.deltaTime > 0f ? displacement.magnitude / Time.deltaTime : 0f;
        bool rolling = active && body.isGrounded && speed > 0.15f && displacement.magnitude < 2f;
        UpdateRollingAudio(rolling, active && body.isGrounded, speed);
        if (rollingDust != null)
        {
            rollingDust.transform.position = Feet();
            var emission = rollingDust.emission;
            emission.enabled = rolling;
            if (rolling && !rollingDust.isPlaying) rollingDust.Play();
        }
        // 밝은 구간의 작은 바위 점프는 하단 경로의 착지 이벤트와 겹치지 않게 감지
        if (!active || (route != null && route.IsFollowing)) { airborneSeconds = fallSpeed = 0f; return; }
        if (!body.isGrounded)
        {
            airborneSeconds += Time.deltaTime;
            fallSpeed = Mathf.Max(fallSpeed, -body.velocity.y);
        }
        else
        {
            if (airborneSeconds > 0.1f && fallSpeed > 1.4f) OnLanded(Feet(), fallSpeed);
            airborneSeconds = fallSpeed = 0f;
        }
    }

    private void UpdateRollingAudio(bool moving, bool grounded, float speed)
    {
        if (rollingAudio == null || settings == null || settings.boulderRoll == null) return;
        if (!grounded) { rollingAudio.Stop(); rollingAudio.volume = 0f; return; }
        float target = moving ? settings.boulderRollVolume * Mathf.Lerp(0.35f, 1f, Mathf.InverseLerp(0.15f, 4f, speed)) : 0f;
        rollingAudio.volume = Mathf.MoveTowards(rollingAudio.volume, target, settings.boulderRollVolume * 6f * Time.deltaTime);
        rollingAudio.pitch = Mathf.Lerp(0.85f, 1.1f, Mathf.InverseLerp(0f, 5f, speed));
        if (moving && !rollingAudio.isPlaying)
        {
            rollingAudio.clip = settings.boulderRoll;
            rollingAudio.Play();
        }
        if (!moving && rollingAudio.volume <= 0f) rollingAudio.Stop();
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
        if (settings != null && impactAudio != null && settings.boulderImpact != null)
            impactAudio.PlayOneShot(settings.boulderImpact, settings.boulderImpactVolume * (major ? 1f : 0.65f));
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
        StopAudio();
    }

    public void BeginEnding()
    {
        ending = true;
        ClearParticles();
        StopAudio();
    }

    private void StopAudio()
    {
        airborneSeconds = fallSpeed = 0f;
        if (rollingAudio != null) { rollingAudio.Stop(); rollingAudio.volume = 0f; }
        if (impactAudio != null) impactAudio.Stop();
    }

    private void ClearParticles()
    {
        foreach (ParticleSystem effect in new[] { impactDust, stoneChips, rollingDust })
            if (effect != null) effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }
}