using System;
using UnityEngine;
using UnityEngine.UI;

public class GeneratorFuelMiniGame : MonoBehaviour
{
    public event Action OnFuelMiniGameCompleted;

    [Header("References")]
    [SerializeField] private InputHandler input;
    [SerializeField] private FPSController playerController;

    [Header("UI")]
    [SerializeField] private GameObject miniGamePanel;
    [SerializeField] private RectTransform timingBar;
    [SerializeField] private RectTransform fillZone;
    [SerializeField] private RectTransform movingMarker;
    [SerializeField] private Image fuelProgressImage;

    [Header("Marker Settings")]
    [SerializeField] private float markerSpeed = 450f;
    [SerializeField] private float markerPadding = 20f;

    [Header("Progress Settings")]
    [SerializeField] private float progressGain = 0.25f;
    [SerializeField] private float progressLoss = 0.15f;

    private float markerDirection = 1f;
    private float fuelProgress;
    private bool isMiniGameActive;
    private bool confirmWasPressed;

    [Header("SFX")]

    public AudioSource audioSource;
    public AudioClip pourSFX;

    [SerializeField] private Animator animator;


    private void Start()
    {
        if (miniGamePanel != null)
        {
            miniGamePanel.SetActive(false);
        }

        UpdateFuelProgressUI();
    }

    private void Update()
    {
        if (!isMiniGameActive) return;
        if (input == null) return;

        MoveMarker();
        HandleInput();
    }

    public void StartMiniGame()
    {
        isMiniGameActive = true;
        markerDirection = 1f;
        confirmWasPressed = false;

        if (input != null)
        {
            input.ResetInputValues();
        }

        if (playerController != null)
        {
            playerController.SetMiniGameMode(true);
        }

        if (miniGamePanel != null)
        {
            miniGamePanel.SetActive(true);
        }

        if (movingMarker != null)
        {
            movingMarker.anchoredPosition = Vector2.zero;
        }

        UpdateFuelProgressUI();
    }

    private void MoveMarker()
    {
        if (timingBar == null || movingMarker == null) return;

        float halfWidth = timingBar.rect.width / 2f;
        float leftLimit = -halfWidth + markerPadding;
        float rightLimit = halfWidth - markerPadding;

        Vector2 position = movingMarker.anchoredPosition;
        position.x += markerDirection * markerSpeed * Time.unscaledDeltaTime;

        if (position.x >= rightLimit)
        {
            position.x = rightLimit;
            markerDirection = -1f;
        }
        else if (position.x <= leftLimit)
        {
            position.x = leftLimit;
            markerDirection = 1f;
        }

        movingMarker.anchoredPosition = position;
    }

    private void HandleInput()
    {
        if (input.timedAction && !confirmWasPressed)
        {
            TryFuelClick();
            confirmWasPressed = true;
        }

        if (!input.timedAction)
        {
            confirmWasPressed = false;
        }

        if (input.cancel)
        {
            input.cancel = false;
            CancelMiniGame();
        }
    }

    private void TryFuelClick()
    {
        if (animator != null)
        {
            animator.SetTrigger("pour");
        }

        if (IsMarkerInsideFillZone())
        {
            fuelProgress += progressGain;

            if (audioSource != null)
            {
                audioSource.clip = pourSFX;
                audioSource.Play();
            }
        }
        else
        {
            fuelProgress -= progressLoss;
        }

        fuelProgress = Mathf.Clamp01(fuelProgress);
        UpdateFuelProgressUI();

        if (fuelProgress >= 1f)
        {
            CompleteMiniGame();
        }
    }

    private bool IsMarkerInsideFillZone()
    {
        if (movingMarker == null || fillZone == null) return false;

        float markerX = movingMarker.anchoredPosition.x;

        float fillZoneCenterX = fillZone.anchoredPosition.x;
        float fillZoneHalfWidth = fillZone.rect.width / 2f;

        float fillZoneLeft = fillZoneCenterX - fillZoneHalfWidth;
        float fillZoneRight = fillZoneCenterX + fillZoneHalfWidth;

        return markerX >= fillZoneLeft && markerX <= fillZoneRight;
    }

    private void UpdateFuelProgressUI()
    {
        if (fuelProgressImage != null)
        {
            fuelProgressImage.fillAmount = fuelProgress;
        }
    }

    private void CompleteMiniGame()
    {
        isMiniGameActive = false;

        if (miniGamePanel != null)
        {
            miniGamePanel.SetActive(false);
        }

        if (input != null)
        {
            input.timedAction = false;
            input.cancel = false;
        }

        if (playerController != null)
        {
            playerController.SetMiniGameMode(false);
        }

        OnFuelMiniGameCompleted?.Invoke();
    }

    public void CancelMiniGame()
    {
        isMiniGameActive = false;

        if (miniGamePanel != null)
        {
            miniGamePanel.SetActive(false);
        }

        if (input != null)
        {
            input.timedAction = false;
            input.cancel = false;
        }

        if (playerController != null)
        {
            playerController.SetMiniGameMode(false);
        }
    }
}
