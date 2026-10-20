using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class ObjectPlacer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private InputHandler input;
    [SerializeField] private PlayerToolInventory inventory;
    [SerializeField] private ToolEquipmentManager equipmentManager;

    [Header("Placement Parameters")]
    [SerializeField] private GameObject previewObjectPrefab;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private LayerMask placementSurfaceLayerMask;

    [Header("Preview Material")]
    [SerializeField] private Material previewMaterial;
    [SerializeField] private Color validColor;
    [SerializeField] private Color invalidColor;

    [Header("Raycast Parameters")]
    [SerializeField] private float objectDistanceFromPlayer;
    [SerializeField] private float raycastStartVerticalOffset;
    [SerializeField] private float raycastDistance;

    private GameObject _previewObject = null;
    private Vector3 _currentPlacementPosition = Vector3.zero;
    private bool _inPlacementMode = false;
    private bool _validPreviewState = false;
    private ToolClass _activeToolToPlace = null;

    [Header("SFX")]
    public AudioClip placementSFX;

    private void Update()
    {
        if (_inPlacementMode)
        {
            UpdateCurrentPlacementPosition();

            if (CanPlaceObject())
                SetValidPreviewState();
            else
                SetInvalidPreviewState();

            UpdateInput();
        }
    }

    private void UpdateInput()
    {
        if (input.placeObject)
        {
            PlaceObject();
            input.placeObject = false;
        }
    }

    private void UpdateCurrentPlacementPosition()
    {
        if (_previewObject == null) return;

        Vector3 cameraForward = new Vector3(playerCamera.transform.forward.x, 0f, playerCamera.transform.forward.z);
        cameraForward.Normalize();

        Vector3 startPos = playerCamera.transform.position + (cameraForward * objectDistanceFromPlayer);
        startPos.y += raycastStartVerticalOffset;

        RaycastHit hitInfo;
        if (Physics.Raycast(startPos, Vector3.down, out hitInfo, raycastDistance, placementSurfaceLayerMask))
        {
            float verticalOffset = GetObjectVerticalExtent(_previewObject);
            Vector3 placementPos = hitInfo.point;
            placementPos.y += verticalOffset;

            _currentPlacementPosition = placementPos;
        }

        Quaternion rotation = Quaternion.Euler(0f, playerCamera.transform.eulerAngles.y, 0f);
        _previewObject.transform.position = _currentPlacementPosition;
        _previewObject.transform.rotation = rotation;
    }

    private float GetObjectVerticalExtent(GameObject obj)
    {
        Renderer objectRenderer = obj.GetComponentInChildren<Renderer>();
        if (objectRenderer != null)
        {
            return objectRenderer.bounds.extents.y;
        }

        Collider objectCollider = obj.GetComponentInChildren<Collider>();
        if (objectCollider != null)
        {
            return objectCollider.bounds.extents.y;
        }

        return 0f;
    }

    private void SetValidPreviewState()
    {
        previewMaterial.color = validColor;
        _validPreviewState = true;
    }

    private void SetInvalidPreviewState()
    {
        previewMaterial.color = invalidColor;
        _validPreviewState = false;
    }

    private bool CanPlaceObject()
    {
        if (_previewObject == null)
            return false;

        PreviewObjectValidChecker checker = _previewObject.GetComponentInChildren<PreviewObjectValidChecker>();
        return checker != null && checker.IsValid;
    }

    private void PlaceObject()
    {
        if (!_inPlacementMode || !_validPreviewState || _activeToolToPlace == null)
            return;

        Quaternion rotation = Quaternion.Euler(0f, playerCamera.transform.eulerAngles.y, 0f);
        GameObject placedObject = Instantiate(_activeToolToPlace.toolPrefab, _currentPlacementPosition, rotation);

        if (equipmentManager != null && equipmentManager.CurrentToolObject != null)
        {
            KeroseneLampScript heldLamp = equipmentManager.CurrentToolObject.GetComponentInChildren<KeroseneLampScript>();
            KeroseneLampScript placedLamp = placedObject.GetComponentInChildren<KeroseneLampScript>();

            if (heldLamp != null && placedLamp != null)
            {
                placedLamp.SetPowerAndIgnitedState(heldLamp.CurrentPower, heldLamp.IsIgnited);
            }
        }

        if (placementSFX != null)
        {
            AudioSource.PlayClipAtPoint(placementSFX, _currentPlacementPosition);
        }

        if (inventory != null)
        {
            inventory.RemoveTool(_activeToolToPlace);
        }

        if (equipmentManager != null)
        {
            equipmentManager.UnequipTool();
        }
    }

    public void EnterPlacementModeFromEquip(ToolClass tool)
    {
        if (_inPlacementMode)
            ExitPlacementMode();

        _activeToolToPlace = tool;
        Quaternion rotation = Quaternion.Euler(0f, playerCamera.transform.eulerAngles.y, 0f);
        _previewObject = Instantiate(previewObjectPrefab, _currentPlacementPosition, rotation, transform);
        _inPlacementMode = true;
    }

    public void ExitPlacementMode()
    {
        if (_previewObject != null)
        {
            Destroy(_previewObject);
        }
        _previewObject = null;
        _inPlacementMode = false;
        _activeToolToPlace = null;
    }
}