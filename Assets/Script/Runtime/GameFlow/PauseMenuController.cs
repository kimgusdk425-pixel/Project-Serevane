using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-100)]
[DisallowMultipleComponent]
public class PauseMenuController : MonoBehaviour
{
    [SerializeField] private GameObject pauseRoot, mainMenu, settingsPanel;
    [SerializeField] private MenuSettingsPanel settingsControls;
    [SerializeField] private UnityEngine.UI.Button resumeButton, settingsButton, titleButton, backButton;
    [SerializeField] private TMP_Text loadingText;
    private PlayerMovement player;
    private float previousTimeScale;
    private bool previousAudioPause, previousCursorVisible, isLoading;
    private CursorLockMode previousCursorLock;
    private static int resumeFrame = -1;
    public static bool IsPaused { get; private set; }
    public static bool BlocksGameplayInput => IsPaused || Time.frameCount == resumeFrame;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetState()
    {
        IsPaused = false;
        resumeFrame = -1;
    }

    private void Awake()
    {
        player = FindFirstObjectByType<PlayerMovement>();
        pauseRoot.SetActive(false);
        loadingText.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        resumeButton.onClick.AddListener(Resume);
        settingsButton.onClick.AddListener(ShowSettings);
        titleButton.onClick.AddListener(ReturnToTitle);
        backButton.onClick.AddListener(BackToPause);
    }

    private void OnDisable()
    {
        resumeButton.onClick.RemoveListener(Resume);
        settingsButton.onClick.RemoveListener(ShowSettings);
        titleButton.onClick.RemoveListener(ReturnToTitle);
        backButton.onClick.RemoveListener(BackToPause);
        if (IsPaused) Restore();
    }

    private void Update()
    {
        if (isLoading || Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame) return;
        if (!IsPaused) Open();
        else if (settingsPanel.activeSelf) BackToPause();
        else Resume();
    }

    private void Open()
    {
        if (player == null || !player.IsControlEnabled || Time.timeScale <= 0f) return;
        previousTimeScale = Time.timeScale;
        previousAudioPause = AudioListener.pause;
        previousCursorLock = Cursor.lockState;
        previousCursorVisible = Cursor.visible;
        IsPaused = true;
        Time.timeScale = 0f;
        AudioListener.pause = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        settingsPanel.SetActive(false);
        mainMenu.SetActive(true);
        pauseRoot.SetActive(true);
        Select(resumeButton.gameObject);
    }

    private void Resume()
    {
        if (!IsPaused || isLoading) return;
        PlayerPrefs.Save();
        pauseRoot.SetActive(false);
        Select(null);
        Restore();
    }

    private void Restore()
    {
        Time.timeScale = previousTimeScale;
        AudioListener.pause = previousAudioPause;
        Cursor.lockState = previousCursorLock;
        Cursor.visible = previousCursorVisible;
        IsPaused = false;
        resumeFrame = Time.frameCount; // 메뉴를 닫은 Space·마우스 입력이 게임에 새지 않음
    }

    private void ShowSettings()
    {
        if (!IsPaused || isLoading) return;
        mainMenu.SetActive(false);
        settingsPanel.SetActive(true);
        Select(settingsControls.FirstSlider.gameObject);
    }

    private void BackToPause()
    {
        if (!IsPaused || isLoading) return;
        PlayerPrefs.Save();
        settingsPanel.SetActive(false);
        mainMenu.SetActive(true);
        Select(settingsButton.gameObject);
    }

    private void ReturnToTitle()
    {
        if (!IsPaused || isLoading) return;
        isLoading = true;
        PlayerPrefs.Save();
        StageTransferState.Clear();
        Restore();
        Time.timeScale = 1f;
        AudioListener.pause = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        player.SetControlEnabled(false);
        resumeButton.interactable = settingsButton.interactable = titleButton.interactable = false;
        loadingText.gameObject.SetActive(true);
        Select(null);
        SceneManager.LoadSceneAsync("Title", LoadSceneMode.Single);
    }

    private static void Select(GameObject target)
    {
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(target);
    }
}
