using UnityEngine;
using UnityEngine.UI;

public class TensionManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject shadowProwler;

    public void ActivateCreature()
    {
        if(shadowProwler != null)
        {
            shadowProwler.SetActive(true);
        }
        
    }
    /*[Header("References")]
    [SerializeField] private PlayerLightStatus playerLightStatus;
    [SerializeField] private CreatureAI creatureAI;
    [SerializeField] private Image tensionFillBar;

    [Header("Tension Settings")]
    [SerializeField] private float maxTension = 100f;
    [SerializeField] private float baseTensionIncreaseRate = 5f;
    [SerializeField] private float baseTensionDecreaseRate = 10f;

    [Header("Territory Scaling")]
    [Tooltip("Multiplier applied to increase rate when 100% of the house is dark.")]
    [SerializeField] private float maxDarknessRateMultiplier = 3.5f;

    private float currentTension;
    private bool hasTriggeredKill;
    private bool isTensionActive;

    public float CurrentTension => currentTension;
    public float MaxTension => maxTension;

    private void OnEnable()
    {
        if (creatureAI != null)
        {
            creatureAI.OnCreatureDisappeared += ResetTension;
        }
    }

    private void OnDisable()
    {
        if (creatureAI != null)
        {
            creatureAI.OnCreatureDisappeared -= ResetTension;
        }
    }

    private void Update()
    {
        if (!isTensionActive) return;
        if (playerLightStatus == null || creatureAI == null || hasTriggeredKill) return;

        HandleTensionCalculation();
        UpdateUI();
        CheckTensionThreshold();
    }

    public void ActivateTensionSystem()
    {
        isTensionActive = true;
    }

    private void HandleTensionCalculation()
    {
        if (playerLightStatus.isInLight)
        {
            currentTension -= baseTensionDecreaseRate * Time.deltaTime;
        }
        else
        {
            float darknessRatio = MapLightManager.Instance != null
                ? MapLightManager.Instance.DarknessPercentage
                : 0f;

            float dynamicRateMultiplier = 1f + (darknessRatio * (maxDarknessRateMultiplier - 1f));

            currentTension += baseTensionIncreaseRate * dynamicRateMultiplier * Time.deltaTime;
        }

        currentTension = Mathf.Clamp(currentTension, 0f, maxTension);
    }

    private void UpdateUI()
    {
        if (tensionFillBar != null)
        {
            tensionFillBar.fillAmount = currentTension / maxTension;
        }
    }

    private void CheckTensionThreshold()
    {
        if (currentTension >= maxTension)
        {
            hasTriggeredKill = true;
            TriggerCreatureKill();
        }
    }

    private void TriggerCreatureKill()
    {
        creatureAI.StartChaseFromBestPoint();
    }

    public void ResetTension()
    {
        currentTension = 0f;
        hasTriggeredKill = false;
        UpdateUI();
    }*/
}