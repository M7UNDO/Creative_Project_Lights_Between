using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    [Header("Time System")]
    public static bool IsGameOver => Instance != null && !Instance.isGameActive;
    [SerializeField] private TextMeshProUGUI clockText;
    [SerializeField] private float realMinutesPerGameHour = 3f;

    private float currentTimeInHours;
    private bool isGameActive;
    private bool hasTimeStarted;

    [Header("UI Panels")]
    [SerializeField] private GameObject winPanel;
    [SerializeField] private GameObject winFirstSelected;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject gameOverFirstSelected;

    [Header("Confirmation Settings")]
    [SerializeField] private GameObject confirmationPanel;
    [SerializeField] private TextMeshProUGUI confirmationText;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private GameObject confirmFirstSelected;

    [Header("Scene Settings")]
    [Space(5)]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    private GameObject activeMainPanel;
    private GameObject activeMainFirstSelected;
    private GameObject lastSelectedBeforeSubMenu;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        Time.timeScale = 1f;
        currentTimeInHours = 0f;
        isGameActive = true;
        hasTimeStarted = false;

        if (winPanel != null) winPanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (confirmationPanel != null) confirmationPanel.SetActive(false);
    }

    public void StartTime()
    {
        hasTimeStarted = true;
        UpdateClockUI();
    }

    public void UpdateClockUI()
    {
        if (clockText == null) return;

        int rawHours = Mathf.FloorToInt(currentTimeInHours);
        int displayHours = rawHours == 0 ? 12 : rawHours;
        int minutes = Mathf.FloorToInt((currentTimeInHours - rawHours) * 60f);

        clockText.text = string.Format("{0:00}:{1:00}AM", displayHours, minutes);
    }

    private void Update()
    {
        if (!isGameActive) return;

        if (hasTimeStarted)
        {
            float realSecondsPerGameHour = realMinutesPerGameHour * 60f;
            float timeSpeedMultiplier = 1f / realSecondsPerGameHour;

            currentTimeInHours += timeSpeedMultiplier * Time.deltaTime;

            UpdateClockUI();

            if (currentTimeInHours >= 6f)
            {
                currentTimeInHours = 6f;
                UpdateClockUI();
                EvaluateTimeUp();
            }
        }
    }

    private void EvaluateTimeUp()
    {
        if (!isGameActive) return;

        isGameActive = false;
        ShowPanel(winPanel, winFirstSelected);
    }

    public void TriggerPlayerDeath()
    {
        if (!isGameActive) return;

        isGameActive = false;
        StartCoroutine(DeathSequenceCoroutine());
    }

    private IEnumerator DeathSequenceCoroutine()
    {
        yield return new WaitForSecondsRealtime(5f);
        ShowPanel(gameOverPanel, gameOverFirstSelected);
    }

    private void ShowPanel(GameObject panel, GameObject firstSelected)
    {
        Time.timeScale = 0f;

        activeMainPanel = panel;
        activeMainFirstSelected = firstSelected;

        if (panel != null)
        {
            panel.SetActive(true);
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        SetSelected(firstSelected);
    }

    private void SetSelected(GameObject obj)
    {
        if (obj == null || EventSystem.current == null) return;
        StartCoroutine(SetSelectedNextFrame(obj));
    }

    private IEnumerator SetSelectedNextFrame(GameObject obj)
    {
        yield return null;
        EventSystem.current.SetSelectedGameObject(null);
        yield return null;
        EventSystem.current.SetSelectedGameObject(obj);
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
            if (activeMainPanel != null) activeMainPanel.SetActive(false);

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
            if (activeMainPanel != null) activeMainPanel.SetActive(true);
            RestorePreviousSelection();
        }
    }

    private void RememberCurrentSelection()
    {
        if (EventSystem.current == null) return;

        GameObject current = EventSystem.current.currentSelectedGameObject;

        if (current != null)
            lastSelectedBeforeSubMenu = current;
        else
            lastSelectedBeforeSubMenu = activeMainFirstSelected;
    }

    private void RestorePreviousSelection()
    {
        if (lastSelectedBeforeSubMenu != null)
            SetSelected(lastSelectedBeforeSubMenu);
        else
            SetSelected(activeMainFirstSelected);
    }

    public void RestartLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void LoadMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}