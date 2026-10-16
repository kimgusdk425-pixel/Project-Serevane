using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerMovement))]
public class PlayerActionFeedback : MonoBehaviour
{
    [SerializeField] private GameplayFeedbackSettings settings;
    [SerializeField] private AudioSource collectionAudio, movementAudio, footstepAudio;
    [SerializeField] private ParticleSystem collectionSparkles;
    private readonly RaycastHit[] groundHits = new RaycastHit[16];
    private Vector3 previousPosition;
    private Animator characterAnimator;
    private int lastFootstep = int.MinValue;
    private static readonly int Walking = Animator.StringToHash("Base Layer.Walk");
    private PlayerMovement movement;
    private PlayerJumpProgress jumpProgress;
    private bool milestonePending;
    private CharacterController body;
    private float airborneSeconds, fallSpeed, suppressLandingUntil, nextFragmentSound, nextFootstepTime;

    private void Awake()
    {
        movement = GetComponent<PlayerMovement>();
        jumpProgress = GetComponent<PlayerJumpProgress>();
        body = GetComponent<CharacterController>();
        characterAnimator = GetComponentInChildren<Animator>();
        if (settings == null || collectionAudio == null || movementAudio == null || collectionSparkles == null)
            Debug.LogWarning("PlayerActionFeedback: 효과 설정·소리·반짝임 연결을 확인하세요.", this);
    }

    private void OnEnable()
    {
        movement.Jumped += OnJumped;
        if (jumpProgress != null) jumpProgress.FragmentCollected += OnFragmentCollected;
        movement.Respawned += ResetMotion;
        ResetMotion();
    }

    private void OnDisable()
    {
        movement.Jumped -= OnJumped;
        if (jumpProgress != null) jumpProgress.FragmentCollected -= OnFragmentCollected;
        movement.Respawned -= ResetMotion;
        ResetMotion();
    }

    public void PlayFragment(Vector3 position)
    {
        if (!isActiveAndEnabled || settings == null) return;
        bool milestone = milestonePending;
        milestonePending = false;
        EmitCollection(position, milestone ? 20 : 9);
        if (milestone && settings.rewardAppear != null && collectionAudio != null)
        {
            nextFragmentSound = Time.time + Mathf.Max(.18f, settings.fragmentSoundInterval);
            collectionAudio.Stop();
            collectionAudio.PlayOneShot(settings.rewardAppear, settings.challengeVolume); // 한 묶음의 성장 순간에 기존 보상음 하나만 강조
            return;
        }
        if (Time.time < nextFragmentSound) return;
        nextFragmentSound = Time.time + settings.fragmentSoundInterval;
        if (collectionAudio != null && settings.fragment != null)
            collectionAudio.PlayOneShot(settings.fragment, settings.fragmentVolume);
    }

    private void OnFragmentCollected()
    {
        int count = jumpProgress.CollectedFragments;
        milestonePending = count % 4 == 0;
    }

    public void PlayKeyPickup(Vector3 position)
    {
        if (!isActiveAndEnabled || settings == null) return;
        EmitCollection(position, 10);
        if (collectionAudio == null || settings.keyPickup == null) return;
        collectionAudio.Stop();
        collectionAudio.PlayOneShot(settings.keyPickup, settings.keyVolume); // 열쇠가 사라져도 플레이어의 소리와 입자는 유지
    }

    private void EmitCollection(Vector3 position, int count)
    {
        if (collectionSparkles != null)
            collectionSparkles.Emit(new ParticleSystem.EmitParams { position = position + Vector3.up * .2f, applyShapeToPosition = true }, count);
    }

    private void OnJumped()
    {
        StopFootsteps();
        if (settings != null && movementAudio != null && settings.jump != null)
            PlayMovementSound(settings.jump, settings.jumpVolume);
    }

    private void LateUpdate()
    {
        Vector3 displacement = transform.position - previousPosition;
        previousPosition = transform.position;
        displacement.y = 0f;
        if (!body.enabled) { StopFootsteps(); return; }
        if (!body.isGrounded)
        {
            StopFootsteps();
            airborneSeconds += Time.deltaTime;
            fallSpeed = Mathf.Max(fallSpeed, -body.velocity.y);
            return;
        }
        if (airborneSeconds > 0.1f && fallSpeed > 1.4f && Time.time >= suppressLandingUntil &&
            settings != null && movementAudio != null && settings.landing != null)
        {
            float strength = Mathf.Lerp(0.55f, 1f, Mathf.InverseLerp(3f, 16f, fallSpeed));
            StopFootsteps();
            nextFootstepTime = Time.time + settings.landing.length; // 착지 한 번에 걷기 소리가 겹치지 않게 함
            PlayMovementSound(settings.landing, settings.landingVolume * strength);
        }
        airborneSeconds = fallSpeed = 0f;
        UpdateFootsteps(displacement.magnitude);
    }

    private void PlayMovementSound(AudioClip clip, float volume)
    {
        movementAudio.Stop();
        movementAudio.clip = clip;
        movementAudio.volume = volume;
        movementAudio.Play(); // 점프·착지는 각각 하나의 소리만 재생
    }

    private void UpdateFootsteps(float distance)
    {
        if (!body.enabled || !body.isGrounded || !movement.IsControlEnabled || distance > 2f)
        {
            StopFootsteps();
            return;
        }
        if (distance < 0.00001f || Time.time < suppressLandingUntil || Time.time < nextFootstepTime)
        {
            lastFootstep = int.MinValue;
            return;
        }
        if (settings == null || footstepAudio == null || characterAnimator == null) return;
        AnimatorStateInfo state = characterAnimator.GetCurrentAnimatorStateInfo(0);
        if (state.fullPathHash != Walking || characterAnimator.IsInTransition(0))
        {
            lastFootstep = int.MinValue;
            return;
        }
        int step = Mathf.FloorToInt((state.normalizedTime - settings.footstepContactPhase) * 2f); // 실제 걷기 주기의 좌·우 발 디딤
        if (lastFootstep == int.MinValue) { lastFootstep = step; return; }
        if (step == lastFootstep) return;
        bool continuous = step == lastFootstep + 1;
        lastFootstep = step;
        if (!continuous) return; // 전환·시간 이동으로 건너뛴 발소리를 몰아서 재생하지 않음
        FootstepSurfaceType surface = FindFootstepSurface();
        var clip = settings.GetFootstep(surface, (step & 1) != 0);
        if (clip != null)
        {
            footstepAudio.clip = clip;
            footstepAudio.volume = settings.GetFootstepVolume(surface);
            footstepAudio.Play();
        }
    }

    private FootstepSurfaceType FindFootstepSurface()
    {
        Bounds bounds = body.bounds;
        float radius = Mathf.Min(bounds.extents.x, bounds.extents.z) * 0.75f;
        Vector3 origin = new Vector3(bounds.center.x, bounds.min.y + radius + 0.25f, bounds.center.z);
        int count = Physics.SphereCastNonAlloc(origin, radius, Vector3.down, groundHits, 0.55f,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore); // 발판 가장자리도 실제 지지면을 찾음
        float nearest = float.PositiveInfinity;
        FootstepSurfaceType result = FootstepSurfaceType.Sand;
        for (int i = 0; i < count; i++)
        {
            RaycastHit hit = groundHits[i];
            if (hit.collider == null || hit.normal.y < 0.5f || hit.distance >= nearest ||
                hit.collider.transform.IsChildOf(transform)) continue;
            var carry = hit.collider.GetComponentInParent<CarryableObject>();
            if (carry != null && carry.IsHeld) continue;
            nearest = hit.distance;
            var marker = hit.collider.GetComponentInParent<FootstepSurface>();
            result = marker != null && marker.isActiveAndEnabled ? marker.Surface : FootstepSurfaceType.Sand;
        }
        return result;
    }

    private void StopFootsteps()
    {
        lastFootstep = int.MinValue;
        if (footstepAudio != null) footstepAudio.Stop();
    }

    private void ResetMotion()
    {
        previousPosition = transform.position;
        milestonePending = false;
        StopFootsteps();
        airborneSeconds = fallSpeed = 0f;
        nextFootstepTime = 0f;
        suppressLandingUntil = Time.time + 0.2f; // 재시작 순간이동을 충격 착지로 재생하지 않음
        if (movementAudio != null) movementAudio.Stop();
        if (collectionAudio != null) collectionAudio.Stop();
        if (collectionSparkles != null)
            collectionSparkles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }
}
