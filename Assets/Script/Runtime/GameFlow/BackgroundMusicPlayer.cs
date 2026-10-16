using System.Collections;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public class BackgroundMusicPlayer : MonoBehaviour
{
    private const float NormalVolume = .2f;
    private const float TransitionSeconds = 2f;
    private AudioSource source, otherSource;
    private AudioClip defaultMusic, explorationMusic, chaseMusic;
    private Coroutine transition;
    private bool initialized;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Create()
    {
        if (FindFirstObjectByType<BackgroundMusicPlayer>() != null) return;
        var go = new GameObject("BackgroundMusic");
        DontDestroyOnLoad(go);
        go.AddComponent<BackgroundMusicPlayer>();
    }

    private void Awake()
    {
        source = GetComponent<AudioSource>();
        otherSource = gameObject.AddComponent<AudioSource>();
        SceneManager.sceneLoaded += RestoreMusic;
    }

    private void OnDestroy() => SceneManager.sceneLoaded -= RestoreMusic;

    private void Start()
    {
        var mixer = Resources.Load<AudioMixer>("GameAudio");
        var group = mixer.FindMatchingGroups("Music")[0];
        foreach (AudioSource channel in new[] { source, otherSource })
        {
            channel.outputAudioMixerGroup = group;
            channel.loop = true;
            channel.ignoreListenerPause = true;
            channel.playOnAwake = false;
            channel.spatialBlend = 0f;
            channel.volume = 0f;
        }
        defaultMusic = Resources.Load<AudioClip>("Audio/TempDesertBgm");
        explorationMusic = Resources.Load<AudioClip>("Audio/Nordic Landscape Ambient Edit");
        if (explorationMusic == null) Debug.LogError("BackgroundMusicPlayer: 탐험 음악 연결을 확인해 주세요.", this);
        chaseMusic = Resources.Load<AudioClip>("Audio/ha-suffocate");
        if (chaseMusic == null) Debug.LogError("BackgroundMusicPlayer: 추격 음악 연결을 확인해 주세요.", this);
        source.clip = MusicForScene(SceneManager.GetActiveScene().name);
        source.volume = NormalVolume;
        GameSettings.ApplyAudio(); // Mixer 설정은 Awake 이후에 적용
        initialized = true;
        if (source.clip != null) source.Play();
    }

    private AudioClip MusicForScene(string sceneName) =>
        sceneName == "MainGame_Chase" && chaseMusic != null ? chaseMusic :
        sceneName == "MainGame_Exploration" && explorationMusic != null ? explorationMusic : defaultMusic;

    private void RestoreMusic(Scene scene, LoadSceneMode mode)
    {
        if (!initialized || mode == LoadSceneMode.Additive) return;
        AudioClip next = MusicForScene(scene.name);
        if (next == null || (source.clip == next && source.isPlaying)) return; // 같은 씬 재시작은 재생 위치 유지
        StopTransition();
        if (source.clip != next)
        {
            if (otherSource.clip == next && otherSource.isPlaying)
            {
                SwapSources(); // 빠르게 되돌아가면 남아 있는 곡을 이어 재생
            }
            else
            {
                if (otherSource.volume > source.volume) SwapSources();
                otherSource.Stop();
                otherSource.clip = next;
                otherSource.volume = 0f;
                otherSource.Play();
                SwapSources();
            }
        }
        else source.Play(); // 엔딩에서 멈춘 음악은 다시 시작
        transition = StartCoroutine(CrossFade());
    }

    private void SwapSources()
    {
        AudioSource previous = source;
        source = otherSource;
        otherSource = previous;
    }

    private void StopTransition()
    {
        if (transition == null) return;
        StopCoroutine(transition);
        transition = null;
    }

    private IEnumerator CrossFade()
    {
        float incoming = source.volume, outgoing = otherSource.volume;
        float elapsed = 0f;
        while (elapsed < TransitionSeconds)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / TransitionSeconds);
            source.volume = Mathf.Lerp(incoming, NormalVolume, t);
            otherSource.volume = Mathf.Lerp(outgoing, 0f, t);
            yield return null;
        }
        source.volume = NormalVolume;
        otherSource.volume = 0f;
        otherSource.Stop();
        transition = null;
    }

    public IEnumerator FadeOut(float duration)
    {
        StopTransition();
        float startVolume = source.volume, otherVolume = otherSource.volume;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            source.volume = Mathf.Lerp(startVolume, 0f, t);
            otherSource.volume = Mathf.Lerp(otherVolume, 0f, t);
            yield return null;
        }
        source.volume = otherSource.volume = 0f;
        source.Stop();
        otherSource.Stop();
    }
}