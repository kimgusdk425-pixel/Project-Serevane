using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public class ObjectActionFeedback : MonoBehaviour
{
    [SerializeField] private GameplayFeedbackSettings settings;
    [SerializeField] private ParticleSystem dust;
    [SerializeField] private Renderer[] visuals = System.Array.Empty<Renderer>();
    private AudioSource source;
    private CarryableObject carry;
    private SlidingDoor sliding;
    private LockedDoor locked;
    private PressurePlate plate;
    private TimedCollectionChallenge challenge;
    private ChallengeFragmentReward reward;
    private MaterialPropertyBlock[] originalBlocks, workingBlocks;
    private Color[] baseColors, baseEmissions;
    private Color pulseColor;
    private bool previousPlateState, previousRetryState;
    private const float PulseDuration = .26f;
    private static readonly Color WarmPulse = new Color(1f, .82f, .43f);
    private static readonly Color TrialPulse = new Color(.3f, .7f, 1f);
    private float pulseRemaining;
    private bool awaitingBoxLanding;
    private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
    private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

    private void Awake()
    {
        source = GetComponent<AudioSource>();
        carry = GetComponent<CarryableObject>();
        sliding = GetComponent<SlidingDoor>();
        locked = GetComponent<LockedDoor>();
        plate = GetComponent<PressurePlate>();
        challenge = GetComponent<TimedCollectionChallenge>();
        reward = GetComponent<ChallengeFragmentReward>();
        originalBlocks = new MaterialPropertyBlock[visuals.Length];
        workingBlocks = new MaterialPropertyBlock[visuals.Length];
        baseColors = new Color[visuals.Length];
        baseEmissions = new Color[visuals.Length];
        for (int i = 0; i < visuals.Length; i++)
        {
            originalBlocks[i] = new MaterialPropertyBlock();
            workingBlocks[i] = new MaterialPropertyBlock();
            if (visuals[i] == null) continue;
            visuals[i].GetPropertyBlock(originalBlocks[i]);
            visuals[i].GetPropertyBlock(workingBlocks[i]);
            baseColors[i] = visuals[i].sharedMaterial != null && visuals[i].sharedMaterial.HasProperty(BaseColor)
                ? visuals[i].sharedMaterial.GetColor(BaseColor) : Color.white;
            baseEmissions[i] = visuals[i].sharedMaterial != null && visuals[i].sharedMaterial.HasProperty(EmissionColor)
                ? visuals[i].sharedMaterial.GetColor(EmissionColor) : Color.black;
        }
    }

    private void OnEnable()
    {
        if (carry != null) { carry.PickedUp += OnPickup; carry.Dropped += OnDrop; }
        if (sliding != null) sliding.MovementStarted += OnDoor;
        if (locked != null) locked.Opened += OnLockedDoor;
        if (plate != null) plate.Activated += OnPlate;
        if (challenge != null)
        {
            challenge.Started += OnChallengeStarted;
            challenge.Failed += OnChallengeFailed;
            if (reward == null) challenge.Completed += OnChallengeCompleted;
        }
        if (reward != null) reward.Revealed += OnChallengeCompleted;
    }

    private void OnDisable()
    {
        if (carry != null) { carry.PickedUp -= OnPickup; carry.Dropped -= OnDrop; }
        if (sliding != null) sliding.MovementStarted -= OnDoor;
        if (locked != null) locked.Opened -= OnLockedDoor;
        if (plate != null) plate.Activated -= OnPlate;
        if (challenge != null)
        {
            challenge.Started -= OnChallengeStarted;
            challenge.Failed -= OnChallengeFailed;
            if (reward == null) challenge.Completed -= OnChallengeCompleted;
        }
        if (reward != null) reward.Revealed -= OnChallengeCompleted;
        awaitingBoxLanding = false;
        pulseRemaining = 0f;
        previousPlateState = previousRetryState = false;
        RestoreColors();
        source.Stop();
        if (dust != null) dust.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    private void OnPickup()
    {
        awaitingBoxLanding = false;
        Pulse();
        if (settings != null && settings.boxPickup != null) PlayCue(settings.boxPickup, settings.boxVolume);
    }

    private void OnDrop()
    {
        awaitingBoxLanding = true; // 실제 바닥 접촉까지 시각/소리 반응을 기다림
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!awaitingBoxLanding || carry == null || carry.IsHeld) return;
        for (int i = 0; i < collision.contactCount; i++)
        {
            ContactPoint contact = collision.GetContact(i);
            if (contact.normal.y < 0.5f) continue;
            awaitingBoxLanding = false;
            Pulse();
            EmitDust(contact.point, 8);
            if (settings != null && settings.boxLanding != null) PlayCue(settings.boxLanding, settings.boxVolume);
            break;
        }
    }

    private void OnPlate()
    {
        Pulse();
        if (settings != null) PlayCue(settings.plateActivate, settings.plateVolume);
    }

    private void OnChallengeStarted()
    {
        Pulse(TrialPulse);
        if (dust != null) dust.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        Vector3 position = visuals.Length > 0 && visuals[0] != null ? visuals[0].bounds.center : transform.position;
        EmitStateSparkles(position + Vector3.up * .2f, 8, TrialPulse);
        if (settings != null) PlayCue(settings.trialStart, settings.challengeVolume);
    }

    private void OnChallengeCompleted()
    {
        Pulse();
        EmitStateSparkles((reward != null ? reward.RewardPosition : transform.position) + Vector3.up * .15f, 12, WarmPulse);
        PlayerMovement player = FindFirstObjectByType<PlayerMovement>();
        if (player != null) EmitStateSparkles(player.transform.position + Vector3.up * .2f, 6, WarmPulse); // 먼 보상까지 돌아가기 전에도 성공을 확인
        if (settings != null) PlayCue(reward != null && settings.rewardAppear != null ? settings.rewardAppear : settings.trialSuccess, settings.challengeVolume);
    }

    private void OnChallengeFailed()
    {
        pulseRemaining = 0f;
        if (dust != null) dust.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (settings != null) PlayCue(settings.trialFail, settings.challengeVolume);
    }

    private void PlayCue(AudioClip clip, float volume)
    {
        if (clip == null || source == null) return;
        source.Stop();
        source.clip = clip;
        source.volume = volume;
        source.Play(); // 상태가 바뀔 때 짧은 효과음 하나만 재생
    }

    private void OnLockedDoor() => PlayDoorFeedback(true);

    private void OnDoor(bool opening) => PlayDoorFeedback(false);

    private void PlayDoorFeedback(bool unlocking)
    {
        Pulse();
        Collider shape = GetComponent<Collider>();
        Vector3 position = shape != null ? new Vector3(shape.bounds.center.x, shape.bounds.min.y + 0.06f, shape.bounds.center.z) : transform.position;
        EmitDust(position, 14);
        if (settings != null) PlayCue(unlocking && settings.keyDoorUnlock != null ? settings.keyDoorUnlock : settings.door, settings.doorVolume);
    }

    private void EmitDust(Vector3 position, int count)
    {
        if (dust != null) dust.Emit(new ParticleSystem.EmitParams { position = position }, count);
    }

    private void EmitStateSparkles(Vector3 position, int count, Color color)
    {
        if (dust != null) dust.Emit(new ParticleSystem.EmitParams { position = position, startColor = color, applyShapeToPosition = true }, count);
    }

    private void Pulse() => Pulse(WarmPulse);
    private void Pulse(Color color) { pulseColor = color; pulseRemaining = PulseDuration; }

    private void LateUpdate()
    {
        bool plateActive = plate != null && plate.IsActivated;
        bool awaitingRetry = challenge != null && challenge.IsAwaitingRetry;
        if (pulseRemaining <= 0f && plateActive == previousPlateState && awaitingRetry == previousRetryState) return;
        pulseRemaining = Mathf.Max(0f, pulseRemaining - Time.deltaTime);
        float amount = Mathf.Sin(Mathf.PI * (1f - pulseRemaining / PulseDuration)) * .55f;
        for (int i = 0; i < visuals.Length; i++)
        {
            if (visuals[i] == null) continue;
            Color settled = plateActive ? Color.Lerp(baseColors[i], WarmPulse, .35f) :
                awaitingRetry ? Color.Lerp(baseColors[i], new Color(.16f, .22f, .28f), .45f) : baseColors[i];
            workingBlocks[i].SetColor(BaseColor, Color.Lerp(settled, pulseColor, amount));
            workingBlocks[i].SetColor(EmissionColor, (plateActive ? WarmPulse * .45f : baseEmissions[i]) + pulseColor * amount * .3f);
            visuals[i].SetPropertyBlock(workingBlocks[i]); // 공용 재질과 충돌체는 유지
        }
        previousPlateState = plateActive;
        previousRetryState = awaitingRetry;
        if (pulseRemaining <= 0f && !plateActive && !awaitingRetry) RestoreColors();
    }

    private void RestoreColors()
    {
        for (int i = 0; i < visuals.Length; i++)
            if (visuals[i] != null) visuals[i].SetPropertyBlock(originalBlocks[i]);
    }
}
