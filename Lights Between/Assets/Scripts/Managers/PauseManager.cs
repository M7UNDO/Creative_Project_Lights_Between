using UnityEngine;
using UnityEngine.Audio;

public class PauseManager : MonoBehaviour
{
    public static bool IsGamePaused { get; private set; }

    [Header("References")]
    [Space(5)]
    [SerializeField] private ManageUI manageUI;
    [SerializeField] private TabController tabController;
    [SerializeField] private FPSController playerController;

    [Header("Audio Mixer Snapshots")]
    [Space(5)]
    [SerializeField] private AudioMixerSnapshot unpausedSnapshot;
    [SerializeField] private AudioMixerSnapshot pausedSnapshot;
    [SerializeField] private float snapshotTransitionDuration = 0.1f;

    [Header("UI Components")]
    [Space(5)]
    public GameObject PauseCanvas;

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

        if (tabController == null)
        {
            tabController = GetComponentInChildren<TabController>(true);
        }
    }

    public void TogglePause()
    {
        if (EnemyManager.IsGameOver) return;

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
        if (EnemyManager.IsGameOver) return;

        IsGamePaused = true;
        Time.timeScale = 0f;

        if (pausedSnapshot != null)
        {
            pausedSnapshot.TransitionTo(snapshotTransitionDuration);
        }

        if (BackgroundMusicManager.Instance != null)
        {
            BackgroundMusicManager.Instance.PauseGameMusic();
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
            BackgroundMusicManager.Instance.ResumeGameMusic();
        }

        if (PauseCanvas != null)
        {
            PauseCanvas.SetActive(false);
        }

        if (!NoteUIController.isNoteOpen)
        {
            if (VolumeBlurController.Instance != null)
            {
                VolumeBlurController.Instance.SetMenuBlurState(false);
            }
        }

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }
}