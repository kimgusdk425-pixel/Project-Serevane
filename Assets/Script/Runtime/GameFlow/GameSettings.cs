using UnityEngine;
using UnityEngine.Audio;

public static class GameSettings
{
    private const string Prefix = "Serevane.";
    private static AudioMixer mixer;
    public static float MasterVolume { get; private set; } = 1f;
    public static float SfxVolume { get; private set; } = 1f;
    public static float MusicVolume { get; private set; } = .65f;
    public static float MouseSensitivity { get; private set; } = 1f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Load()
    {
        MasterVolume = Read("MasterVolume", 1f, 0f, 1f);
        SfxVolume = Read("SfxVolume", 1f, 0f, 1f);
        MusicVolume = Read("MusicVolume", .65f, 0f, 1f);
        MouseSensitivity = Read("MouseSensitivity", 1f, .25f, 2f);
        mixer = Resources.Load<AudioMixer>("GameAudio");
        AudioListener.volume = MasterVolume;
    }

    private static float Read(string name, float fallback, float min, float max)
    {
        float value = PlayerPrefs.GetFloat(Prefix + name, fallback);
        return float.IsNaN(value) || float.IsInfinity(value) ? fallback : Mathf.Clamp(value, min, max);
    }

    public static void SetMasterVolume(float value)
    {
        MasterVolume = Mathf.Clamp01(value);
        AudioListener.volume = MasterVolume;
        PlayerPrefs.SetFloat(Prefix + "MasterVolume", MasterVolume);
    }

    public static void SetSfxVolume(float value)
    {
        SfxVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(Prefix + "SfxVolume", SfxVolume);
        SetMixerVolume("SfxVolume", SfxVolume);
    }

    public static void SetMusicVolume(float value)
    {
        MusicVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(Prefix + "MusicVolume", MusicVolume);
        SetMixerVolume("MusicVolume", MusicVolume);
    }

    public static void SetMouseSensitivity(float value)
    {
        MouseSensitivity = Mathf.Clamp(value, .25f, 2f);
        PlayerPrefs.SetFloat(Prefix + "MouseSensitivity", MouseSensitivity);
    }

    public static void ApplyAudio()
    {
        SetMixerVolume("SfxVolume", SfxVolume);
        SetMixerVolume("MusicVolume", MusicVolume);
    }

    private static void SetMixerVolume(string name, float value)
    {
        if (mixer != null) mixer.SetFloat(name, value <= 0f ? -80f : 20f * Mathf.Log10(value));
    }
}
