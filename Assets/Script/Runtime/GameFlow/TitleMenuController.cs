using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class TitleMenuController : MonoBehaviour
{
    [SerializeField] private string gameScene = "MainGame_Exploration";
    [SerializeField] private GameObject mainMenu, settingsPanel;
    [SerializeField] private Button startButton, settingsButton, quitButton, backButton;
    [SerializeField] private MenuSettingsPanel settingsControls;
    [SerializeField] private TMP_Text loadingText;
    private bool isStarting;

    private void Awake()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        mainMenu.SetActive(true);
        settingsPanel.SetActive(false);
        loadingText.gameObject.SetActive(false);

    }

    private void OnEnable()
    {
        startButton.onClick.AddListener(StartGame);
        settingsButton.onClick.AddListener(ShowSettings);
        quitButton.onClick.AddListener(QuitGame);
        backButton.onClick.AddListener(BackToMenu);
    }

    private void OnDisable()
    {
        startButton.onClick.RemoveListener(StartGame);
        settingsButton.onClick.RemoveListener(ShowSettings);
        quitButton.onClick.RemoveListener(QuitGame);
        backButton.onClick.RemoveListener(BackToMenu);
    }

    private void Start() => Select(startButton.gameObject);

    private void Update()
    {
        if (!isStarting && settingsPanel.activeSelf && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            BackToMenu();
    }

    private void ShowSettings()
    {
        if (isStarting) return;
        mainMenu.SetActive(false);
        settingsPanel.SetActive(true);
        Select(settingsControls.FirstSlider.gameObject);
    }

    private void BackToMenu()
    {
        PlayerPrefs.Save();
        settingsPanel.SetActive(false);
        mainMenu.SetActive(true);
        Select(settingsButton.gameObject);
    }

    private void StartGame()
    {
        if (isStarting) return;
        isStarting = true;
        PlayerPrefs.Save();
        StageTransferState.Clear();
        startButton.interactable = settingsButton.interactable = quitButton.interactable = false;
        loadingText.gameObject.SetActive(true);
        SceneManager.LoadSceneAsync(gameScene, LoadSceneMode.Single);
    }

    private void QuitGame()
    {
        PlayerPrefs.Save();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private static void Select(GameObject target)
    {
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(target);
    }
}
