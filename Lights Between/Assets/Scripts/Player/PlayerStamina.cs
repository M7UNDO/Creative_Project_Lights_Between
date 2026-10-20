using UnityEngine;
using UnityEngine.UI;

public class PlayerStamina : MonoBehaviour
{
    [Header("Stamina Settings")]
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float sprintDuration = 4f;
    [SerializeField] private float regenDuration = 3f;
    [SerializeField] private float regenDelay = 0.5f;

    [Header("UI References")]
    [SerializeField] private CanvasGroup staminaCanvasGroup;
    [SerializeField] private Image staminaFill;

    [Header("UI Fading")]
    [SerializeField] private float fadeSpeed = 8f;
    [SerializeField] private float hideDelay = 2f;

    public float CurrentStamina { get; private set; }

    private float timeSinceLastSprint;
    private float timeAtMaxStamina;
    private float targetAlpha;

    private void Start()
    {
        CurrentStamina = maxStamina;
        if (staminaCanvasGroup != null) staminaCanvasGroup.alpha = 0f;
    }

    private void Update()
    {
        UpdateUI();
    }

    public void RefillStamina()
    {
        CurrentStamina = maxStamina;
        timeAtMaxStamina = hideDelay;
        targetAlpha = 0f;
        if (staminaCanvasGroup != null)
        {
            staminaCanvasGroup.alpha = 0f;
        }
    }

    public void ForceHideUI()
    {
        targetAlpha = 0f;
        if (staminaCanvasGroup != null)
        {
            staminaCanvasGroup.alpha = 0f;
        }
    }

    public void ProcessStamina(bool isSprinting, bool suppressUI = false)
    {
        float drainRate = maxStamina / sprintDuration;
        float regenRate = maxStamina / regenDuration;

        if (isSprinting)
        {
            CurrentStamina -= drainRate * Time.deltaTime;
            timeSinceLastSprint = 0f;
            timeAtMaxStamina = 0f;
            targetAlpha = suppressUI ? 0f : 1f;
        }
        else
        {
            timeSinceLastSprint += Time.deltaTime;

            if (timeSinceLastSprint >= regenDelay && CurrentStamina < maxStamina)
            {
                CurrentStamina += regenRate * Time.deltaTime;
                targetAlpha = suppressUI ? 0f : 1f;
            }
        }

        CurrentStamina = Mathf.Clamp(CurrentStamina, 0f, maxStamina);

        if (suppressUI)
        {
            targetAlpha = 0f;
        }
        else if (CurrentStamina >= maxStamina && !isSprinting)
        {
            timeAtMaxStamina += Time.deltaTime;
            if (timeAtMaxStamina >= hideDelay)
            {
                targetAlpha = 0f;
            }
        }
    }

    private void UpdateUI()
    {
        if (staminaFill != null)
        {
            staminaFill.fillAmount = CurrentStamina / maxStamina;
        }

        if (staminaCanvasGroup != null)
        {
            staminaCanvasGroup.alpha = Mathf.MoveTowards(
                staminaCanvasGroup.alpha,
                targetAlpha,
                fadeSpeed * Time.deltaTime
            );
        }
    }
}