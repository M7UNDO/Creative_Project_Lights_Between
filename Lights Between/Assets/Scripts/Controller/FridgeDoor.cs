using UnityEngine;

public class FridgeDoor : RotatingOpenable
{
    [Header("Electrical Grid Connection")]
    [SerializeField] private ElectricBoxBreaker connectedBreaker;

    [Header("Fridge Interior Light")]
    [SerializeField] private Light interiorLight;

    protected override void Awake()
    {
        base.Awake();

        if (connectedBreaker == null)
        {
            connectedBreaker = Object.FindFirstObjectByType<ElectricBoxBreaker>();
        }
    }

    private void OnEnable()
    {
        if (connectedBreaker != null)
        {
            connectedBreaker.OnPowerGridStateChanged += HandlePowerGridStateChanged;
        }
    }

    private void OnDisable()
    {
        if (connectedBreaker != null)
        {
            connectedBreaker.OnPowerGridStateChanged -= HandlePowerGridStateChanged;
        }
    }

    private void Start()
    {
        RefreshFridgeLightState();
    }

    public override void Interact()
    {
        base.Interact();
        RefreshFridgeLightState();
    }

    private void HandlePowerGridStateChanged(bool isPowerActive)
    {
        RefreshFridgeLightState();
    }

    private void RefreshFridgeLightState()
    {
        bool hasPower = connectedBreaker != null && connectedBreaker.IsGridActive;

        if (interiorLight != null)
        {
            interiorLight.enabled = isOpen && hasPower;
        }
    }
}