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
    private MaterialPropertyBlock[] originalBlocks, workingBlocks;
    private Color[] baseColors;
    private float pulseRemaining;
    private bool awaitingBoxLanding;
    private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

    private void Awake()
    {
        source = GetComponent<AudioSource>();
        carry = GetComponent<CarryableObject>();
        sliding = GetComponent<SlidingDoor>();
        locked = GetComponent<LockedDoor>();
        plate = GetComponent<PressurePlate>();
        challenge = GetComponent<TimedCollectionChallenge>();
        originalBlocks = new MaterialPropertyBlock[visuals.Length];
        workingBlocks = new MaterialPropertyBlock[visuals.Length];
        baseColors = new Color[visuals.Length];
        for (int i = 0; i < visuals.Length; i++)
        {
            originalBlocks[i] = new MaterialPropertyBlock();
            workingBlocks[i] = new MaterialPropertyBlock();
            if (visuals[i] == null) continue;
            visuals[i].GetPropertyBlock(originalBlocks[i]);
            visuals[i].GetPropertyBlock(workingBlocks[i]);
            baseColors[i] = visuals[i].sharedMaterial != null && visuals[i].sharedMaterial.HasProperty(BaseColor)
                ? visuals[i].sharedMaterial.GetColor(BaseColor) : Color.white;
        }
    }

    private void OnEnable()
    {
        if (carry != null) { carry.PickedUp += OnPickup; carry.Dropped += OnDrop; }
        if (sliding != null) sliding.MovementStarted += OnDoor;
        if (locked != null) locked.Opened += OnLockedDoor;
        if (plate != null) plate.Activated += OnPlate;
        if (challenge != null) { challenge.Started += OnChallengeStarted; challenge.Completed += OnChallengeCompleted; challenge.Failed += OnChallengeFailed; }
    }

    private void OnDisable()
    {
        if (carry != null) { carry.PickedUp -= OnPickup; carry.Dropped -= OnDrop; }
        if (sliding != null) sliding.MovementStarted -= OnDoor;
        if (locked != null) locked.Opened -= OnLockedDoor;
        if (plate != null) plate.Activated -= OnPlate;
        if (challenge != null) { challenge.Started -= OnChallengeStarted; challenge.Completed -= OnChallengeCompleted; challenge.Failed -= OnChallengeFailed; }
        awaitingBoxLanding = false;
        pulseRemaining = 0f;
        RestoreColors();
        source.Stop();
        if (dust != null) dust.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    private void OnPickup()
    {
        awaitingBoxLanding = false;
        Pulse();
        if (settings != null && settings.boxPickup != null) source.PlayOneShot(settings.boxPickup, settings.boxVolume);
    }

    private void OnDrop()
    {
        awaitingBoxLanding = true; // 놓기 성공 뒤 바닥에 실제 닿았을 때 한 번만 소리
        Pulse();
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
            if (settings != null && settings.boxLanding != null) source.PlayOneShot(settings.boxLanding, settings.boxVolume);
            break;
        }
    }

    private void OnPlate() { if (settings != null) PlayCue(settings.plateActivate, settings.plateVolume); }
    private void OnChallengeStarted() { if (settings != null) PlayCue(settings.trialStart, settings.challengeVolume); }
    private void OnChallengeCompleted() { if (settings != null) PlayCue(settings.trialSuccess, settings.challengeVolume); }
    private void OnChallengeFailed() { if (settings != null) PlayCue(settings.trialFail, settings.challengeVolume); }

    private void PlayCue(AudioClip clip, float volume)
    {
        if (clip == null || source == null) return;
        source.Stop();
        source.clip = clip;
        source.volume = volume;
        source.Play(); // 상태가 바뀔 때 짧은 효과음 하나만 재생
    }

    private void OnLockedDoor() => OnDoor(true);

    private void OnDoor(bool opening)
    {
        Pulse();
        Collider shape = GetComponent<Collider>();
        Vector3 position = shape != null ? new Vector3(shape.bounds.center.x, shape.bounds.min.y + 0.06f, shape.bounds.center.z) : transform.position;
        EmitDust(position, 14);
        if (settings != null && settings.door != null) source.PlayOneShot(settings.door, settings.doorVolume);
    }

    private void EmitDust(Vector3 position, int count)
    {
        if (dust != null) dust.Emit(new ParticleSystem.EmitParams { position = position }, count);
    }

    private void Pulse() => pulseRemaining = 0.22f;

    private void LateUpdate()
    {
        if (pulseRemaining <= 0f) return;
        pulseRemaining = Mathf.Max(0f, pulseRemaining - Time.deltaTime);
        float amount = Mathf.Sin(Mathf.PI * (1f - pulseRemaining / 0.22f)) * 0.35f;
        for (int i = 0; i < visuals.Length; i++)
        {
            if (visuals[i] == null) continue;
            workingBlocks[i].SetColor(BaseColor, Color.Lerp(baseColors[i], new Color(1f, 0.82f, 0.43f), amount));
            visuals[i].SetPropertyBlock(workingBlocks[i]); // 공용 머티리얼과 충돌체 크기는 바꾸지 않음
        }
        if (pulseRemaining <= 0f) RestoreColors();
    }

    private void RestoreColors()
    {
        for (int i = 0; i < visuals.Length; i++)
            if (visuals[i] != null) visuals[i].SetPropertyBlock(originalBlocks[i]);
    }
}
