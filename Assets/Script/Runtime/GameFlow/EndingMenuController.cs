using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
[RequireComponent(typeof(CanvasGroup))]
public class EndingMenuController : MonoBehaviour
{
    [SerializeField] private UnityEngine.UI.Button replayButton, titleButton, quitButton;
    [SerializeField] private TMP_Text loadingText;
    private CanvasGroup group;
    private bool isReady, isLoading;

    private void OnEnable()
    {
        group = GetComponent<CanvasGroup>();
        isReady = isLoading = false;
        group.interactable = group.blocksRaycasts = false;
        replayButton.interactable = titleButton.interactable = quitButton.interactable = true;
        loadingText.gameObject.SetActive(false);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        replayButton.onClick.AddListener(Replay);
        titleButton.onClick.AddListener(ReturnToTitle);
        quitButton.onClick.AddListener(Quit);
    }

    private void OnDisable()
    {
        replayButton.onClick.RemoveListener(Replay);
        titleButton.onClick.RemoveListener(ReturnToTitle);
        quitButton.onClick.RemoveListener(Quit);
    }

    private void Update()
    {
        if (isReady || isLoading || group.alpha < .99f) return;
        isReady = true;
        group.interactable = group.blocksRaycasts = true;
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(replayButton.gameObject);
    }

    private bool BeginTransition()
    {
        if (!isReady || isLoading) return false;
        isLoading = true;
        isReady = false;
        PlayerPrefs.Save();
        StageTransferState.Clear();
        Time.timeScale = 1f;
        AudioListener.pause = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        group.interactable = false;
        replayButton.interactable = titleButton.interactable = quitButton.interactable = false;
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        return true;
    }

    private void Replay() => Load("MainGame_Exploration");
    private void ReturnToTitle() => Load("Title");

    private void Load(string scene)
    {
        if (!BeginTransition()) return;
        loadingText.gameObject.SetActive(true);
        SceneManager.LoadSceneAsync(scene, LoadSceneMode.Single);
    }

    private void Quit()
    {
        if (!BeginTransition()) return;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
