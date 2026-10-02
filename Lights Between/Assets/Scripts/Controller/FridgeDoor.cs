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

        if (isOpen)
        {
            RefreshFridgeLightState();
        }
    }

    private void Update()
    {
        if (animationCoroutine != null && !isOpen)
        {
            RefreshFridgeLightState();
        }
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
            bool isMoving = animationCoroutine != null;
            bool shouldBeOn = (isOpen || isMoving) && hasPower;

            interiorLight.enabled = shouldBeOn;
        }
    }
}