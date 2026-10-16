using System.Collections;
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
    [SerializeField] private CanvasGroup menuContents;
    private CanvasGroup group;
    private bool isReady, isLoading;

    private void OnEnable()
    {
        group = GetComponent<CanvasGroup>();
        isReady = isLoading = false;
        group.interactable = group.blocksRaycasts = false;
        if (menuContents != null) menuContents.alpha = 0f;
        replayButton.interactable = titleButton.interactable = quitButton.interactable = true;
        loadingText.gameObject.SetActive(false);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        replayButton.onClick.AddListener(Replay);
        titleButton.onClick.AddListener(ReturnToTitle);
        quitButton.onClick.AddListener(Quit);
        StartCoroutine(RevealMenu());
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        replayButton.onClick.RemoveListener(Replay);
        titleButton.onClick.RemoveListener(ReturnToTitle);
        quitButton.onClick.RemoveListener(Quit);
    }

    private IEnumerator RevealMenu()
    {
        while (group.alpha < .99f) yield return null;
        if (menuContents != null)
        {
            yield return new WaitForSecondsRealtime(.5f); // 완료 문구를 먼저 읽을 시간
            float elapsed = 0f;
            while (elapsed < .5f)
            {
                elapsed += Time.unscaledDeltaTime;
                menuContents.alpha = Mathf.SmoothStep(0f, 1f, elapsed / .5f);
                yield return null;
            }
            menuContents.alpha = 1f;
        }
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
