using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class MenuSettingsPanel : MonoBehaviour
{
    [SerializeField] private UnityEngine.UI.Slider volumeSlider, sfxSlider, musicSlider, sensitivitySlider;
    [SerializeField] private TMP_Text volumeValue, sfxValue, musicValue, sensitivityValue;
    public UnityEngine.UI.Slider FirstSlider => volumeSlider;

    private void OnEnable()
    {
        volumeSlider.SetValueWithoutNotify(GameSettings.MasterVolume);
        sfxSlider.SetValueWithoutNotify(GameSettings.SfxVolume);
        musicSlider.SetValueWithoutNotify(GameSettings.MusicVolume);
        sensitivitySlider.SetValueWithoutNotify(GameSettings.MouseSensitivity);
        volumeValue.text = Percent(GameSettings.MasterVolume);
        sfxValue.text = Percent(GameSettings.SfxVolume);
        musicValue.text = Percent(GameSettings.MusicVolume);
        sensitivityValue.text = GameSettings.MouseSensitivity.ToString("0.00") + "×";
        volumeSlider.onValueChanged.AddListener(SetVolume);
        sfxSlider.onValueChanged.AddListener(SetSfxVolume);
        musicSlider.onValueChanged.AddListener(SetMusicVolume);
        sensitivitySlider.onValueChanged.AddListener(SetSensitivity);
    }

    private void OnDisable()
    {
        volumeSlider.onValueChanged.RemoveListener(SetVolume);
        sfxSlider.onValueChanged.RemoveListener(SetSfxVolume);
        musicSlider.onValueChanged.RemoveListener(SetMusicVolume);
        sensitivitySlider.onValueChanged.RemoveListener(SetSensitivity);
    }

    private static string Percent(float value) => Mathf.RoundToInt(value * 100f) + "%";
    private void SetVolume(float value) { GameSettings.SetMasterVolume(value); volumeValue.text = Percent(GameSettings.MasterVolume); }
    private void SetSfxVolume(float value) { GameSettings.SetSfxVolume(value); sfxValue.text = Percent(GameSettings.SfxVolume); }
    private void SetMusicVolume(float value) { GameSettings.SetMusicVolume(value); musicValue.text = Percent(GameSettings.MusicVolume); }
    private void SetSensitivity(float value) { GameSettings.SetMouseSensitivity(value); sensitivityValue.text = GameSettings.MouseSensitivity.ToString("0.00") + "×"; }
}
