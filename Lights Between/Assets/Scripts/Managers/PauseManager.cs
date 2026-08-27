using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

public class PauseScript : MonoBehaviour
{
    public static bool IsPaused { get; private set; }

    [Header("References")]
    [Space(5)]
    [SerializeField] private TabController tabController;
    [SerializeField] private BackgroundMusicManager musicManager;

    [Header("Menu Panels")]
    [Space(5)]
    [SerializeField] private GameObject pauseCanvas;
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject confirmationPanel;

    [Header("Confirmation Settings")]
    [SerializeField] private TextMeshProUGUI confirmationText;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;

    [Header("Canvas Groups")]
    [Space(5)]
    [SerializeField] private CanvasGroup pauseCanvasGroup;

    [Header("First Selected Objects")]
    [Space(5)]
    [SerializeField] private GameObject pauseFirstSelected;
    [SerializeField] private GameObject settingsFirstSelected;
    [SerializeField] private GameObject confirmFirstSelected;

    [Header("Scene Settings")]
    [Space(5)]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    private GameObject lastSelectedBeforeSubMenu;
    private Coroutine selectCoroutine;
    private int lastProcessedFrame = -1;

    [Header("Input")]
    [Space(5)]
    [SerializeField] private InputActionReference pauseInput;
    [SerializeField] private InputActionReference backInput;
    [SerializeField] private InputActionReference selectInput;

    private void Start()
    {
        if (tabController == null)
        {
            tabController = GetComponentInChildren<TabController>(true);
        }

        if (tabController != null && selectInput != null)
        {
            tabController.InitializeTabNavigation(selectInput);
        }

        if (musicManager == null)
        {
            musicManager = FindFirstObjectByType<BackgroundMusicManager>();
        }

        ForceResume();
    }

    private void OnEnable()
    {
        if (pauseInput != null)
        {
            pauseInput.action.Enable();
            pauseInput.action.performed += OnPausePressed;
        }

        if (backInput != null)
        {
            backInput.action.Enable();
            backInput.action.performed += OnBackPressed;
        }
    }

    private void OnDisable()
    {
        if (pauseInput != null)
        {
            pauseInput.action.performed -= OnPausePressed;
            pauseInput.action.Disable();
        }

        if (backInput != null)
        {
            backInput.action.performed -= OnBackPressed;
            backInput.action.Disable();
        }
    }

    private void OnPausePressed(InputAction.CallbackContext context)
    {
        if (LevelManager.IsGameOver) return;
        if (Time.frameCount == lastProcessedFrame) return;

        if (!IsPaused)
        {
            PauseGame();
            return;
        }

        Back();
    }

    private void OnBackPressed(InputAction.CallbackContext context)
    {
        if (!IsPaused) return;
        if (Time.frameCount == lastProcessedFrame) return;

        Back();
    }

    public void TogglePause()
    {
        if (IsPaused)
            ResumeGame();
        else
            PauseGame();
    }

    public void Back()
    {
        lastProcessedFrame = Time.frameCount;

        if (confirmationPanel != null && confirmationPanel.activeSelf)
        {
            CancelConfirmation();
            return;
        }

        if (settingsPanel != null && settingsPanel.activeSelf)
        {
            if (tabController != null && tabController.TryGoBackToTab())
            {
                return;
            }

            CloseSettings();
            return;
        }

        if (pausePanel != null && pausePanel.activeSelf)
        {
            ResumeGame();
        }
    }

    public void PauseGame()
    {
        lastProcessedFrame = Time.frameCount;
        IsPaused = true;
        Time.timeScale = 0f;

        /*if (musicManager != null && musicManager.levelScene)
        {
            musicManager.PauseMusic();
        }*/

        if (pauseCanvas != null) pauseCanvas.SetActive(true);
        if (pausePanel != null) pausePanel.SetActive(true);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (confirmationPanel != null) confirmationPanel.SetActive(false);

        SetPauseCanvasVisible(true);
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        SetSelected(pauseFirstSelected);
    }

    public void ResumeGame()
    {
        ForceResume();
    }

    private void ForceResume()
    {
        IsPaused = false;
        Time.timeScale = 1f;

        /*if (musicManager != null && musicManager.levelScene)
        {
            musicManager.UnpauseMusic();
        }*/

        if (pausePanel != null) pausePanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (confirmationPanel != null) confirmationPanel.SetActive(false);
        if (pauseCanvas != null) pauseCanvas.SetActive(false);

        SetPauseCanvasVisible(false);
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    public void OpenSettings()
    {
        RememberCurrentSelection();

        if (pausePanel != null) pausePanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(true);

        SetSelected(settingsFirstSelected);
    }

    public void CloseSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(true);

        RestorePreviousSelection();
    }

    public void ToggleSettings()
    {
        if (settingsPanel != null && settingsPanel.activeSelf)
            CloseSettings();
        else
            OpenSettings();
    }

    public void RestartLevel()
    {
        //SimpleTutorialPromptManager.ResetTutorialSession();
        ForceResume();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void LoadMainMenu()
    {
        //SimpleTutorialPromptManager.ResetTutorialSession();
        ForceResume();
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void ConfirmQuitGame()
    {
        OpenConfirmationModal("Are you sure you want to exit the game?", QuitGame);
    }

    public void ConfirmMainMenu()
    {
        OpenConfirmationModal("Are you sure you want to exit to main menu?", LoadMainMenu);
    }

    private void OpenConfirmationModal(string message, UnityEngine.Events.UnityAction onConfirmAction)
    {
        RememberCurrentSelection();

        if (confirmationPanel != null)
        {
            if (pausePanel != null) pausePanel.SetActive(false);

            confirmationPanel.SetActive(true);

            if (confirmationText != null)
            {
                confirmationText.text = message;
            }

            if (confirmButton != null)
            {
                confirmButton.onClick.RemoveAllListeners();
                confirmButton.onClick.AddListener(onConfirmAction);
            }

            if (cancelButton != null)
            {
                cancelButton.onClick.RemoveAllListeners();
                cancelButton.onClick.AddListener(CancelConfirmation);
            }

            SetSelected(confirmFirstSelected);
        }
    }

    public void CancelConfirmation()
    {
        if (confirmButton != null) confirmButton.onClick.RemoveAllListeners();
        if (cancelButton != null) cancelButton.onClick.RemoveAllListeners();

        if (confirmationPanel != null)
        {
            confirmationPanel.SetActive(false);
            if (pausePanel != null) pausePanel.SetActive(true);
            RestorePreviousSelection();
        }
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    private void RememberCurrentSelection()
    {
        if (EventSystem.current == null) return;

        GameObject current = EventSystem.current.currentSelectedGameObject;

        if (current != null)
            lastSelectedBeforeSubMenu = current;
        else
            lastSelectedBeforeSubMenu = pauseFirstSelected;
    }

    private void RestorePreviousSelection()
    {
        if (lastSelectedBeforeSubMenu != null)
            SetSelected(lastSelectedBeforeSubMenu);
        else
            SetSelected(pauseFirstSelected);
    }

    private void SetSelected(GameObject obj)
    {
        if (obj == null || EventSystem.current == null) return;

        if (selectCoroutine != null)
            StopCoroutine(selectCoroutine);

        selectCoroutine = StartCoroutine(SetSelectedNextFrame(obj));
    }

    private IEnumerator SetSelectedNextFrame(GameObject obj)
    {
        yield return null;
        EventSystem.current.SetSelectedGameObject(null);
        yield return null;
        EventSystem.current.SetSelectedGameObject(obj);
    }

    private void SetPauseCanvasVisible(bool visible)
    {
        if (pauseCanvasGroup == null) return;

        pauseCanvasGroup.alpha = visible ? 1f : 0f;
        pauseCanvasGroup.interactable = visible;
        pauseCanvasGroup.blocksRaycasts = visible;
    }
}