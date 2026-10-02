using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class ManageUI : MonoBehaviour
{
    [Header("References")]
    [Space(5)]
    [SerializeField] private PauseManager pauseScript;
    [SerializeField] private TabController tabController;
    [SerializeField] private BackgroundMusicManager musicManager;

    [Header("UI Elements")]
    [Space(5)]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject confirmationPanel;

    [SerializeField] private TextMeshProUGUI confirmationText;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;

    [Header("First Selected Objects")]
    [Space(5)]
    [SerializeField] private GameObject pauseFirstSelected;
    [SerializeField] private GameObject settingsFirstSelected;
    [SerializeField] private GameObject confirmFirstSelected;

    [Header("Canvas Groups")]
    [Space(5)]
    [SerializeField] private CanvasGroup pauseCanvasGroup;

    [Header("Scene Settings")]
    [Space(5)]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    private GameObject lastSelectedBeforeSubMenu;
    private Coroutine selectCoroutine;
    private int lastProcessedFrame = -1;

    private void Start()
    {
        if (pauseScript == null)
        {
            pauseScript = FindFirstObjectByType<PauseManager>();
        }

        if (tabController == null)
        {
            tabController = GetComponentInChildren<TabController>(true);
        }

        if (pausePanel != null && pausePanel.activeSelf)
        {
            if (pauseScript != null)
            {
                pauseScript.Resume();
            }
        }

        CloseSettings();

    }

    public void Back()
    {
        if (Time.frameCount == lastProcessedFrame) return;
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
            if (pauseScript != null)
            {
                pauseScript.Resume();
            }
        }
    }

    public void Settings()
    {
        bool isOpening = settingsPanel != null && !settingsPanel.activeSelf;

        if (isOpening)
        {
            OpenSettings();
        }
        else
        {
            CloseSettings();
        }
    }

    public void OpenSettings()
    {
        if (pausePanel != null) pausePanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(true);
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
        }
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public void LoadMainMenu()
    {
        if (pauseScript != null)
        {
            pauseScript.Resume();
        }
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void LoadLevelNumber(int levelIndex)
    {
        if (pauseScript != null)
        {
            pauseScript.Resume();
        }
        SceneManager.LoadScene(levelIndex);
    }

    public void SetPauseCanvasVisible(bool visible)
    {
        if (pauseCanvasGroup == null) return;

        pauseCanvasGroup.alpha = visible ? 1f : 0f;
        pauseCanvasGroup.interactable = visible;
        pauseCanvasGroup.blocksRaycasts = visible;
    }
}