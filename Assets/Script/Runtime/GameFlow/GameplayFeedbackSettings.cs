using UnityEngine;

[CreateAssetMenu(menuName = "Serevane/Gameplay Feedback")]
public class GameplayFeedbackSettings : ScriptableObject
{
    public AudioClip keyPickup, plateActivate, trialStart, trialSuccess, trialFail;
    [Range(0f, 1f)] public float keyVolume = 0.22f;
    [Range(0f, 1f)] public float plateVolume = 0.3f;
    [Range(0f, 1f)] public float challengeVolume = 0.22f;
    public AudioClip fragment, jump, landing, boxPickup, boxLanding, door;
    public AudioClip footstepA, footstepB, boulderRoll, boulderImpact;
    public AudioClip stoneFootstepA, stoneFootstepB, woodFootstepA, woodFootstepB;
    [Range(0f, 1f)] public float stoneFootstepVolume = 0.14f;
    [Range(0f, 1f)] public float woodFootstepVolume = 0.16f;
    [Range(0f, 1f)] public float footstepVolume = 0.18f;
    [Range(0f, 0.5f)] public float footstepContactPhase = 0f; // 실제 플레이어 발 디딤: 주기 0과 0.5
    [Range(0f, 1f)] public float boulderRollVolume = 0.32f;
    [Range(0f, 1f)] public float boulderImpactVolume = 0.55f;
    [Range(0f, 1f)] public float fragmentVolume = 0.24f;
    [Range(0f, 1f)] public float jumpVolume = 0.24f;
    [Range(0f, 1f)] public float landingVolume = 0.35f;
    [Range(0f, 1f)] public float boxVolume = 0.4f;
    [Range(0f, 1f)] public float doorVolume = 0.4f;
    [Min(0f)] public float fragmentSoundInterval = 0.09f; // 한 무리의 조각 소리가 한꺼번에 겹치지 않게 함

    public AudioClip GetFootstep(FootstepSurfaceType surface, bool rightFoot)
    {
        AudioClip clip = null;
        if (surface == FootstepSurfaceType.Stone) clip = rightFoot ? stoneFootstepB : stoneFootstepA;
        else if (surface == FootstepSurfaceType.Wood) clip = rightFoot ? woodFootstepB : woodFootstepA;
        return clip != null ? clip : (rightFoot ? footstepB : footstepA);
    }

    public float GetFootstepVolume(FootstepSurfaceType surface)
    {
        if (surface == FootstepSurfaceType.Stone) return stoneFootstepVolume;
        if (surface == FootstepSurfaceType.Wood) return woodFootstepVolume;
        return footstepVolume;
    }
}
