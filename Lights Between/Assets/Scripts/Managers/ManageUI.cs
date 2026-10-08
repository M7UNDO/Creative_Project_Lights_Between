using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ManageUI : MonoBehaviour
{
    [Header("References", order = 0)]
    [Space(5)]
    [SerializeField] private PauseManager pauseScript;
    [SerializeField] private TabController tabController;
    [SerializeField] private InputHandler inputHandler;
    [SerializeField] private InventoryUI inventoryUI;

    [Header("UI Elements", order = 1)]
    [Space(5)]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject confirmationPanel;

    [SerializeField] private TextMeshProUGUI confirmationText;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;

    [Header("Audio", order = 2)]
    [Space(5)]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip closeSFX;

    [Header("First Selected Objects", order = 3)]
    [Space(5)]
    [SerializeField] private GameObject pauseFirstSelected;
    [SerializeField] private GameObject settingsFirstSelected;
    [SerializeField] private GameObject confirmFirstSelected;

    [Header("Canvas Groups", order = 4)]
    [Space(5)]
    [SerializeField] private CanvasGroup pauseCanvasGroup;

    [Header("Scene Settings", order = 5)]
    [Space(5)]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    private GameObject lastSelectedButton;

    private void Awake()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

    private void Start()
    {
        if (pauseScript == null)
        {
            pauseScript = FindFirstObjectByType<PauseManager>();
        }

        if (inputHandler == null)
        {
            inputHandler = FindFirstObjectByType<InputHandler>();
        }

        if (inventoryUI == null)
        {
            inventoryUI = FindFirstObjectByType<InventoryUI>();
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

    private void Update()
    {
        if (inputHandler == null) return;

        if (inputHandler.inventory)
        {
            inputHandler.inventory = false;
            HandleInventoryInput();
            return;
        }

        if (inputHandler.back || inputHandler.pause)
        {
            inputHandler.back = false;
            inputHandler.pause = false;
            Back();
        }
    }

    private void HandleInventoryInput()
    {
        if (confirmationPanel != null && confirmationPanel.activeSelf) return;
        if (settingsPanel != null && settingsPanel.activeSelf) return;

        if (InventoryUI.IsInventoryOpen)
        {
            if (inventoryUI != null)
            {
                inventoryUI.CloseInventory();
            }
        }
        else if (!PauseManager.IsGamePaused)
        {
            if (inventoryUI != null)
            {
                inventoryUI.OpenInventory();
            }
        }
    }

    public void Back()
    {
        if (confirmationPanel != null && confirmationPanel.activeSelf)
        {
            CancelConfirmation();
            return;
        }

        if (settingsPanel != null && settingsPanel.activeSelf)
        {
            CloseSettings();
            return;
        }

        if (InventoryUI.IsInventoryOpen)
        {
            if (inventoryUI != null)
            {
                if (inventoryUI.HasSelectedItem())
                {
                    inventoryUI.DeselectItem();
                }
                else
                {
                    inventoryUI.CloseInventory();
                }
            }
            return;
        }

        if (PauseManager.IsGamePaused)
        {
            PlayCloseSFX();
            if (pauseScript != null)
            {
                pauseScript.Resume();
            }
            return;
        }

        if (pauseScript != null)
        {
            pauseScript.Pause();
            SetSelected(pauseFirstSelected);
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
        SaveCurrentSelected();

        if (pausePanel != null) pausePanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(true);

        SetSelected(settingsFirstSelected);
    }

    public void CloseSettings()
    {
        PlayCloseSFX();
        if (settingsPanel != null) settingsPanel.SetActive(false);

        if (pausePanel != null && PauseManager.IsGamePaused)
        {
            pausePanel.SetActive(true);
            RestorePreviousSelected();
        }
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
            SaveCurrentSelected();

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
        PlayCloseSFX();

        if (confirmButton != null) confirmButton.onClick.RemoveAllListeners();
        if (cancelButton != null) cancelButton.onClick.RemoveAllListeners();

        if (confirmationPanel != null)
        {
            confirmationPanel.SetActive(false);

            if (pausePanel != null && PauseManager.IsGamePaused)
            {
                pausePanel.SetActive(true);
                RestorePreviousSelected();
            }
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

    private void SaveCurrentSelected()
    {
        if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null)
        {
            lastSelectedButton = EventSystem.current.currentSelectedGameObject;
        }
    }

    private void RestorePreviousSelected()
    {
        if (lastSelectedButton != null)
        {
            SetSelected(lastSelectedButton);
            lastSelectedButton = null;
        }
        else
        {
            SetSelected(pauseFirstSelected);
        }
    }

    private void SetSelected(GameObject target)
    {
        if (target != null && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(target);
        }
    }

    private void PlayCloseSFX()
    {
        if (audioSource != null && closeSFX != null)
        {
            audioSource.PlayOneShot(closeSFX);
        }
    }
}