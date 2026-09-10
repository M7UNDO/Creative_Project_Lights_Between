using UnityEngine;

public class KeroseneLampScript : MonoBehaviour, IToolPower
{
    [Header("References")]
    [SerializeField] private GameObject lampLightObject;
    [SerializeField] private GameObject lampLightZone;

    [Header("Drain Settings")]
    [SerializeField] private float drainRate = 2f;
    [SerializeField] private float defaultMaxPower = 100f;

    private ToolClass toolData;
    private float currentPower;
    private float maxPower;
    private bool isEquipped;

    public float CurrentPower => currentPower;
    public float MaxPower => maxPower > 0f ? maxPower : defaultMaxPower;
    public bool UsesPower => toolData != null ? toolData.usesPower : true;

    private void Awake()
    {
        if (maxPower <= 0f)
        {
            maxPower = defaultMaxPower;
            currentPower = maxPower;
        }
    }

    public void Initialise(ToolClass tool)
    {
        toolData = tool;
        maxPower = tool.maxPower;
        currentPower = maxPower;
        isEquipped = true;

        if (lampLightObject != null)
        {
            lampLightObject.SetActive(true);
        }

        if (lampLightZone != null)
        {
            lampLightZone.SetActive(true);
        }
    }

    private void Update()
    {
        if (!isEquipped && toolData != null) return;
        if (currentPower <= 0f) return;

        currentPower -= drainRate * Time.deltaTime;
        currentPower = Mathf.Clamp(currentPower, 0f, maxPower);

        if (currentPower <= 0f)
        {
            if (lampLightObject != null)
            {
                lampLightObject.SetActive(false);
            }

            if (lampLightZone != null)
            {
                lampLightZone.SetActive(false);
            }
        }
    }
}