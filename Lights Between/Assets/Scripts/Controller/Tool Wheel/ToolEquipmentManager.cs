using System.Collections.Generic;
using UnityEditor.EditorTools;
using UnityEngine;

public class ToolEquipmentManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform toolHoldPoint;
    [SerializeField] private ObjectPlacer objectPlacer;
    [SerializeField] private ToolUIManager toolUIManager;

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
        if (toolUIManager == null)
        {
            toolUIManager = FindFirstObjectByType<ToolUIManager>();
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

        UpdateActiveToolUI();
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
            }
        }
    }

    public void UnequipTool()
    {
        if (objectPlacer != null)
        {
            objectPlacer.ExitPlacementMode();
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

        if (toolUIManager != null)
        {
            toolUIManager.UpdateToolUI(null, null, 0);
        }
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

    private void UpdateActiveToolUI()
    {
        if (toolUIManager == null || currentTool == null) return;

        toolUIManager.UpdateToolUI(currentTool, currentToolInstance, 1);
    }
}