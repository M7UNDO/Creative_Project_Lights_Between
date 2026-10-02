using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class MainManager : MonoBehaviour
{
    [Header("References")]
    [Space(5)]
    [SerializeField] private PlayerControls playerControls;
    [SerializeField] private TabController tabController;

    [Header("Menu Panels")]
    [Space(5)]
    [SerializeField] private GameObject startGamePanel;
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject creditsPanel;
    [SerializeField] private GameObject settingsPanel;

    [Header("Canvas Groups")]
    [Space(5)]
    [SerializeField] private CanvasGroup startCanvasGroup;
    [SerializeField] private CanvasGroup menuCanvasGroup;

    [Header("Menu Settings")]
    [Space(5)]
    [SerializeField] private float transitionDuration = 1f;
    private GameObject lastSelectedBeforeSubMenu;
    private Coroutine selectCoroutine;

    [Header("Input")]
    [Space(5)]
    [SerializeField] private InputActionReference startInput;
    [SerializeField] private InputActionReference backInput;

    public TextMeshProUGUI startText;
    public float startFlashSpeed = 0.5f;
    private Coroutine flashCoroutine;

    [Header("Camera Transition")]
    [Space(5)]
    [SerializeField] private Transform menuCamera;
    [SerializeField] private Vector3 startRotation;
    [SerializeField] private Vector3 gameRotation;

    [Header("Managers")]
    [Space(5)]
    [SerializeField] private MenuMusicManager musicManager;

    [Header("SFX")]
    [Space(5)]
    public AudioSource startGameSource;

    private static bool hasStartedGame = false;
    private static bool hasSeenIntro = false;

    private void Awake()
    {
        playerControls = new PlayerControls();

        if (!hasSeenIntro)
        {
            if (menuCamera != null)
                startRotation = menuCamera.rotation.eulerAngles;
        }
        else
        {
            if (menuCamera != null)
            {
                menuCamera.rotation = Quaternion.Euler(gameRotation);
            }
        }
    }

    private void Start()
    {
        if (hasSeenIntro)
        {
            startGamePanel.SetActive(false);
            mainMenuPanel.SetActive(true);
            creditsPanel.SetActive(false);
            settingsPanel.SetActive(false);

            SetMenuCanvasVisible(true);

            if (musicManager != null)
            {
                musicManager.StartMusic();
            }
        }
        else
        {
            ShowStartGamePanel();
        }

        if (tabController == null)
        {
            tabController = GetComponentInChildren<TabController>(true);
        }

        if (tabController != null && playerControls != null)
        {
            tabController.InitializeTabNavigation(
                playerControls.UI.PreviousTab,
                playerControls.UI.NextTab
            );
        }

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    private void OnEnable()
    {
        if (playerControls == null)
        {
            playerControls = new PlayerControls();
        }

        playerControls.UI.Enable();

        if (startInput != null && startInput.action != null)
        {
            startInput.action.Enable();
            startInput.action.performed += OnStartPressed;
        }

        if (backInput != null && backInput.action != null)
        {
            backInput.action.Enable();
            backInput.action.performed += OnBackPressed;
        }
    }

    private void OnDisable()
    {
        if (playerControls != null)
        {
            playerControls.UI.Disable();
        }

        if (startInput != null && startInput.action != null)
        {
            startInput.action.performed -= OnStartPressed;
            startInput.action.Disable();
        }

        if (backInput != null && backInput.action != null)
        {
            backInput.action.performed -= OnBackPressed;
            backInput.action.Disable();
        }
    }

    private IEnumerator FlashStart()
    {
        if (startText == null) yield break;

        Color baseColor = startText.color;

        while (!hasStartedGame)
        {
            float t = 0f;

            while (t < startFlashSpeed)
            {
                t += Time.unscaledDeltaTime;
                float alpha = Mathf.Lerp(1f, 0f, t / startFlashSpeed);
                startText.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
                yield return null;
            }

            t = 0f;

            while (t < startFlashSpeed)
            {
                t += Time.unscaledDeltaTime;
                float alpha = Mathf.Lerp(0f, 1f, t / startFlashSpeed);
                startText.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
                yield return null;
            }
        }

        startText.color = new Color(baseColor.r, baseColor.g, baseColor.b, 1f);
    }

    public void ResetMenuState()
    {
        mainMenuPanel.SetActive(true);
        creditsPanel.SetActive(false);
        settingsPanel.SetActive(false);

        SetMenuCanvasVisible(true);
    }

    public void ShowStartGamePanel()
    {
        hasStartedGame = false;

        if (flashCoroutine != null)
            StopCoroutine(flashCoroutine);

        flashCoroutine = StartCoroutine(FlashStart());

        startGamePanel.SetActive(true);
        mainMenuPanel.SetActive(false);
        creditsPanel.SetActive(false);
        settingsPanel.SetActive(false);

        SetStartCanvasVisible(true);
        SetMenuCanvasVisible(false);

        ResetCamera();
    }

    public void StartGame()
    {
        hasSeenIntro = true;
        hasStartedGame = true;

        if (flashCoroutine != null)
            StopCoroutine(flashCoroutine);

        if (startText != null)
        {
            startText.color = new Color(startText.color.r, startText.color.g, startText.color.b, 1f);
        }

        if (musicManager != null)
        {
            musicManager.StartMusic();
        }

        StartCoroutine(StartGameTransition());
    }

    private void OnStartPressed(InputAction.CallbackContext context)
    {
        if (!startGamePanel.activeSelf) return;
        if (hasStartedGame) return;

        hasStartedGame = true;
        StartGame();
    }

    private IEnumerator StartGameTransition()
    {
        if (startGameSource != null)
            startGameSource.Play();

        yield return StartCoroutine(FadeStartCanvas(0f));
        yield return StartCoroutine(RotateCamera(gameRotation));

        startGamePanel.SetActive(false);

        SetMenuCanvasVisible(true);

        mainMenuPanel.SetActive(true);
        creditsPanel.SetActive(false);
        settingsPanel.SetActive(false);
    }

    private IEnumerator FadeStartCanvas(float targetAlpha)
    {
        if (startCanvasGroup == null) yield break;

        float startAlpha = startCanvasGroup.alpha;
        float t = 0f;

        startCanvasGroup.blocksRaycasts = true;

        while (t < transitionDuration)
        {
            t += Time.unscaledDeltaTime;
            float normalized = t / transitionDuration;
            startCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, normalized);
            yield return null;
        }

        startCanvasGroup.alpha = targetAlpha;
        startCanvasGroup.blocksRaycasts = targetAlpha > 0f;
    }

    private IEnumerator RotateCamera(Vector3 targetEuler)
    {
        if (menuCamera == null)
            yield break;

        Quaternion startRot = menuCamera.rotation;
        Quaternion targetRot = Quaternion.Euler(targetEuler);

        float t = 0f;

        while (t < transitionDuration)
        {
            t += Time.unscaledDeltaTime;
            float normalized = t / transitionDuration;
            menuCamera.rotation = Quaternion.Lerp(startRot, targetRot, normalized);
            yield return null;
        }

        menuCamera.rotation = targetRot;
    }

    private void ResetCamera()
    {
        if (menuCamera != null)
            menuCamera.rotation = Quaternion.Euler(startRotation);
    }

    private void OnBackPressed(InputAction.CallbackContext context)
    {
        Back();
    }

    public void Back()
    {
        if (settingsPanel.activeSelf)
        {
            CloseSettings();
            return;
        }

        if (creditsPanel.activeSelf)
        {
            CloseCredits();
            return;
        }
    }

    public void OpenSettings()
    {
        settingsPanel.SetActive(true);
        SetMenuCanvasVisible(false);
    }

    public void CloseSettings()
    {
        settingsPanel.SetActive(false);
        mainMenuPanel.SetActive(true);
        SetMenuCanvasVisible(true);
    }

    public void OpenCredits()
    {
        creditsPanel.SetActive(true);
        SetMenuCanvasVisible(false);
    }

    public void CloseCredits()
    {
        creditsPanel.SetActive(false);
        mainMenuPanel.SetActive(true);
        SetMenuCanvasVisible(true);
    }

    private void SetMenuCanvasVisible(bool visible)
    {
        if (menuCanvasGroup == null) return;

        menuCanvasGroup.alpha = visible ? 1f : 0f;
        menuCanvasGroup.interactable = visible;
        menuCanvasGroup.blocksRaycasts = visible;
    }

    private void SetStartCanvasVisible(bool visible)
    {
        if (startCanvasGroup == null) return;

        startCanvasGroup.alpha = visible ? 1f : 0f;
        startCanvasGroup.interactable = visible;
        startCanvasGroup.blocksRaycasts = visible;
    }

    public void LoadLevel(int levelIndex)
    {
        if (musicManager != null)
        {
            musicManager.FadeOutAndStop(1f);
        }

        SceneManager.LoadScene(levelIndex);
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}