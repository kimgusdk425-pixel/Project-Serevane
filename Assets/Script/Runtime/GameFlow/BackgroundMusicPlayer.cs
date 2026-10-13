using UnityEngine;
using UnityEngine.Audio;

[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public class BackgroundMusicPlayer : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Create()
    {
        var go = new GameObject("BackgroundMusic");
        DontDestroyOnLoad(go);
        go.AddComponent<BackgroundMusicPlayer>();
    }

    private void Start()
    {
        var source = GetComponent<AudioSource>();
        var mixer = Resources.Load<AudioMixer>("GameAudio");
        source.clip = Resources.Load<AudioClip>("Audio/TempDesertBgm");
        source.outputAudioMixerGroup = mixer.FindMatchingGroups("Music")[0];
        source.loop = true;
        source.ignoreListenerPause = true;
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.volume = .2f; // 효과음을 가리지 않는 임시 사막 배경 음악
        GameSettings.ApplyAudio(); // Mixer 설정은 Awake 이후에 적용
        if (source.clip != null) source.Play();
    }
}
