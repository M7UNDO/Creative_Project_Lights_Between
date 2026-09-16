using UnityEngine;
using UnityEngine.InputSystem;

public class PauseManager : MonoBehaviour
{
    public static bool IsGamePaused { get; private set; }

    [Header("References")]
    [Space(5)]
    [SerializeField] private ManageUI manageUI;
    [SerializeField] private TabController tabController;

    [Header("UI Components")]
    [Space(5)]
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
            playerControls.UI.Cancel.performed -= cancelAction;

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

        if (PauseCanvas != null)
        {
            PauseCanvas.SetActive(true);
        }

        if(VolumeBlurController.Instance != null)
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

        if (PauseCanvas != null)
        {
            PauseCanvas.SetActive(false);
        }

        VolumeBlurController.Instance.SetMenuBlurState(false);

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        playerControls.UI.Disable();
        playerControls.Player.Enable();
    }
}