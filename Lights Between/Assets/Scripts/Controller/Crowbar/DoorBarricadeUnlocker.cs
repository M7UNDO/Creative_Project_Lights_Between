using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoorBarricadeUnlocker : MonoBehaviour
{
    [Header("Blocker Settings")]
    [SerializeField] private List<BarricadePlankScript> blockingPlanks = new List<BarricadePlankScript>();
    [SerializeField] private LayerMask nonInteractableLayer;
    [SerializeField] private LayerMask targetInteractionLayer;

    [SerializeField] GameObject doorWorldUI;

    private InputHandler playerInput;

    private void Start()
    {
        SetDoorLayer(GetLayerFromMask(nonInteractableLayer));
        if (doorWorldUI != null) doorWorldUI.SetActive(false);

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerInput = player.GetComponent<InputHandler>();
        }

        CheckPlanksState();
    }

    private void Update()
    {
        CheckPlanksState();
    }

    private void CheckPlanksState()
    {
        blockingPlanks.RemoveAll(plank => plank == null || plank.IsDetached);

        if (blockingPlanks.Count == 0)
        {
            StartCoroutine(UnlockDoorWhenInputReleased());
            enabled = false;

            if(doorWorldUI != null) doorWorldUI.SetActive(true);

        }
    }

    private IEnumerator UnlockDoorWhenInputReleased()
    {
        // Wait until player releases the interaction key so the hold doesn't register on the door
        while (playerInput != null && playerInput.interact)
        {
            yield return null;
        }

        // Small extra frame buffer before making door interactable
        yield return null;

        SetDoorLayer(GetLayerFromMask(targetInteractionLayer));
    }

    private void SetDoorLayer(int layerIndex)
    {
        if (gameObject.layer != layerIndex)
        {
            gameObject.layer = layerIndex;
        }
    }

    private int GetLayerFromMask(LayerMask mask)
    {
        int bitmask = mask.value;
        if (bitmask == 0) return gameObject.layer;

        int layerIndex = 0;
        while (bitmask > 1)
        {
            bitmask >>= 1;
            layerIndex++;
        }
        return layerIndex;
    }
}