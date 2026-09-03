using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToolEquipmentManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform toolHoldPoint;
    [SerializeField] private ObjectPlacer objectPlacer;

    [Header("Current Tool")]
    [SerializeField] private ToolClass currentTool;

    private GameObject currentToolInstance;
    private FlashlightScript currentFlashlight;
    private InputHandler input;

    private Vector3 defaultHoldPointPosition;
    private Quaternion defaultHoldPointRotation;

    private Dictionary<ToolClass, GameObject> instantiatedTools = new Dictionary<ToolClass, GameObject>();

    public ToolClass CurrentTool => currentTool;
    public GameObject CurrentToolObject => currentToolInstance;
    public bool HasToolEquipped => currentTool != null;

    private void Awake()
    {
        input = GetComponent<InputHandler>();
        if (objectPlacer == null)
        {
            objectPlacer = GetComponent<ObjectPlacer>();
        }

        if (toolHoldPoint != null)
        {
            defaultHoldPointPosition = toolHoldPoint.localPosition;
            defaultHoldPointRotation = toolHoldPoint.localRotation;
        }
    }

    private void Start()
    {
        UnequipTool();
    }

    private void Update()
    {
        if (input == null) return;

        if (input.toggleFlashlight)
        {
            ToggleCurrentTool();
            input.toggleFlashlight = false;
        }
    }

    public void EquipTool(ToolClass tool)
    {
        if (tool == null)
        {
            UnequipTool();
            return;
        }

        UnequipTool();

        currentTool = tool;

        if (currentTool.toolType == ToolType.ParaffinLamp)
        {
            if (objectPlacer != null)
            {
                objectPlacer.EnterPlacementModeFromEquip(currentTool);
            }
            return;
        }

        if (tool.toolPrefab == null || toolHoldPoint == null)
        {
            return;
        }

        toolHoldPoint.localPosition = tool.holdPointPositionOffset;
        toolHoldPoint.localRotation = Quaternion.Euler(tool.holdPointRotationOffset);

        if (instantiatedTools.TryGetValue(tool, out GameObject existingInstance))
        {
            currentToolInstance = existingInstance;
            currentToolInstance.transform.localPosition = Vector3.zero;
            currentToolInstance.transform.localRotation = Quaternion.identity;
            currentToolInstance.SetActive(true);

            currentFlashlight = currentToolInstance.GetComponentInChildren<FlashlightScript>();
            if (currentFlashlight != null)
            {
                currentFlashlight.SetEquippedState(true);
            }
        }
        else
        {
            currentToolInstance = Instantiate(
                tool.toolPrefab,
                toolHoldPoint.position,
                toolHoldPoint.rotation,
                toolHoldPoint
            );

            currentToolInstance.transform.localPosition = Vector3.zero;
            currentToolInstance.transform.localRotation = Quaternion.identity;

            instantiatedTools.Add(tool, currentToolInstance);

            currentFlashlight = currentToolInstance.GetComponentInChildren<FlashlightScript>();

            if (currentFlashlight != null)
            {
                currentFlashlight.Initialise(tool);
                currentFlashlight.SetEquippedState(true);
            }
        }
    }

    public void UnequipTool()
    {
        if (objectPlacer != null)
        {
            objectPlacer.ExitPlacementMode();
        }

        if (currentFlashlight != null)
        {
            currentFlashlight.SetEquippedState(false);
        }

        if (currentToolInstance != null)
        {
            currentToolInstance.SetActive(false);
        }

        if (toolHoldPoint != null)
        {
            toolHoldPoint.localPosition = defaultHoldPointPosition;
            toolHoldPoint.localRotation = defaultHoldPointRotation;
        }

        currentToolInstance = null;
        currentTool = null;
        currentFlashlight = null;
    }

    private void ToggleCurrentTool()
    {
        if (currentFlashlight == null) return;

        currentFlashlight.Toggle();
    }

    public bool IsEquipped(ToolClass tool)
    {
        return currentTool == tool;
    }
}