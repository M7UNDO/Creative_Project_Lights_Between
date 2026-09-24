using System;
using System.Collections;
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
    [SerializeField] private Image buttonIconImage;

    [Header("Feedback Settings")]
    [SerializeField] private Color successColor = Color.green;
    [SerializeField] private Color failureColor = Color.red;
    [SerializeField] private float flashDuration = 0.2f;

    [Header("Progress bar Colour")]
    [SerializeField] private Color low = Color.red;
    [SerializeField] private Color mid = Color.yellow;
    [SerializeField] private Color high = Color.green;

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

    private Color originalIconColor = Color.white;
    private Coroutine iconFlashCoroutine;
    private Coroutine progressFlashCoroutine;

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

        if (buttonIconImage != null) originalIconColor = buttonIconImage.color;

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
        fuelProgress = 0f;

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

        ResetUIColors();
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

        if (input.close)
        {
            input.close = false;
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
            TriggerIconFlash(successColor);

            if (audioSource != null && pourSFX != null)
            {
                audioSource.clip = pourSFX;
                audioSource.Play();
            }
        }
        else
        {
            fuelProgress -= progressLoss;
            TriggerIconFlash(failureColor);
            TriggerProgressFlash(failureColor);
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


    private Color GetCurrentProgressColor()
    {
        if (fuelProgress < 0.5f)
        {

            return Color.Lerp(low, mid, fuelProgress * 2f);
        }
        else
        {
            return Color.Lerp(mid,high, (fuelProgress - 0.5f) * 2f);
        }
    }

    private void UpdateFuelProgressUI()
    {
        if (fuelProgressImage != null)
        {
            fuelProgressImage.fillAmount = fuelProgress;
            if (progressFlashCoroutine == null)
            {
                fuelProgressImage.color = GetCurrentProgressColor();
            }
        }
    }

    private void TriggerIconFlash(Color flashColor)
    {
        if (buttonIconImage == null) return;
        if (iconFlashCoroutine != null) StopCoroutine(iconFlashCoroutine);
        iconFlashCoroutine = StartCoroutine(FlashIconCoroutine(flashColor));
    }

    private void TriggerProgressFlash(Color flashColor)
    {
        if (fuelProgressImage == null) return;
        if (progressFlashCoroutine != null) StopCoroutine(progressFlashCoroutine);
        progressFlashCoroutine = StartCoroutine(FlashProgressCoroutine(flashColor));
    }

    private IEnumerator FlashIconCoroutine(Color flashColor)
    {
        buttonIconImage.color = flashColor;
        yield return new WaitForSeconds(flashDuration);
        buttonIconImage.color = originalIconColor;
        iconFlashCoroutine = null;
    }

    private IEnumerator FlashProgressCoroutine(Color flashColor)
    {
        fuelProgressImage.color = flashColor;
        yield return new WaitForSeconds(flashDuration);
        fuelProgressImage.color = GetCurrentProgressColor(); // Restore to lerped color, not white
        progressFlashCoroutine = null;
    }

    private void ResetUIColors()
    {
        if (buttonIconImage != null) buttonIconImage.color = originalIconColor;
        if (fuelProgressImage != null) fuelProgressImage.color = GetCurrentProgressColor();
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
            input.close = false;
        }

        if (playerController != null)
        {
            playerController.SetMiniGameMode(false);
        }

        ResetUIColors();
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
            input.close = false;
        }

        if (playerController != null)
        {
            playerController.SetMiniGameMode(false);
        }

        ResetUIColors();
    }
}