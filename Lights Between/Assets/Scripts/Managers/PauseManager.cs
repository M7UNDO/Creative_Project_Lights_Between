using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;

public class PauseManager : MonoBehaviour
{
    public static bool IsGamePaused { get; private set; }

    [Header("References")]
    [SerializeField] private ManageUI manageUI;
    [SerializeField] private TabController tabController;
    [SerializeField] private FPSController playerController;
    private PlayerInteraction playerInteraction;

    [Header("Audio Mixer Snapshots")]
    [SerializeField] private AudioMixerSnapshot unpausedSnapshot;
    [SerializeField] private AudioMixerSnapshot pausedSnapshot;
    [SerializeField] private float snapshotTransitionDuration = 0.1f;

    [Header("UI Components")]
    public GameObject PauseCanvas;

    private PlayerControls playerControls;
    private System.Action<InputAction.CallbackContext> pauseAction;
    private System.Action<InputAction.CallbackContext> cancelAction;

    private void Awake()
    {
        playerControls = new PlayerControls();
        pauseAction = ctx => TogglePause();
        cancelAction = ctx => OnCancelPressed();
    }

    private void Start()
    {
        if (manageUI == null)
        {
            manageUI = FindFirstObjectByType<ManageUI>();
        }

        if (playerController == null)
        {
            playerController = FindFirstObjectByType<FPSController>();
        }

        if (playerController != null)
        {
            playerInteraction = playerController.GetComponent<PlayerInteraction>();
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
    }

    private void OnEnable()
    {
        if (playerControls == null)
        {
            playerControls = new PlayerControls();
            pauseAction = ctx => TogglePause();
            cancelAction = ctx => OnCancelPressed();
        }

        playerControls.Player.Pause.performed += pauseAction;
        playerControls.UI.Back.performed += cancelAction;

        if (IsGamePaused)
        {
            playerControls.Player.Disable();
            playerControls.UI.Enable();
        }
        else
        {
            playerControls.UI.Disable();
            playerControls.Player.Enable();
        }
    }

    private void OnDisable()
    {
        if (playerControls != null)
        {
            playerControls.Player.Pause.performed -= pauseAction;
            playerControls.UI.Back.performed -= cancelAction;

            playerControls.Player.Disable();
            playerControls.UI.Disable();
        }
    }

    private void OnCancelPressed()
    {
        if (manageUI != null)
        {
            manageUI.Back();
        }
        else
        {
            TogglePause();
        }
    }

    public void TogglePause()
    {
        if (IsGamePaused)
        {
            Resume();
        }
        else
        {
            Pause();
        }
    }

    public void Pause()
    {
        IsGamePaused = true;
        Time.timeScale = 0f;

        if (pausedSnapshot != null)
        {
            pausedSnapshot.TransitionTo(snapshotTransitionDuration);
        }

        if (BackgroundMusicManager.Instance != null)
        {
            BackgroundMusicManager.Instance.PauseMusic();
        }

        if (PauseCanvas != null)
        {
            PauseCanvas.SetActive(true);
        }

        if (VolumeBlurController.Instance != null)
        {
            VolumeBlurController.Instance.SetMenuBlurState(true);
        }

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        playerControls.Player.Disable();
        playerControls.UI.Enable();
    }

    public void Resume()
    {
        IsGamePaused = false;
        Time.timeScale = 1f;

        if (unpausedSnapshot != null)
        {
            unpausedSnapshot.TransitionTo(snapshotTransitionDuration);
        }

        if (BackgroundMusicManager.Instance != null)
        {
            BackgroundMusicManager.Instance.ResumeMusic();
        }

        if (PauseCanvas != null)
        {
            PauseCanvas.SetActive(false);
        }

        if (VolumeBlurController.Instance != null)
        {
            VolumeBlurController.Instance.SetMenuBlurState(false);
        }

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        playerControls.UI.Disable();
        playerControls.Player.Enable();
    }
}