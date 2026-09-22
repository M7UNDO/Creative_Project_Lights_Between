using UnityEngine;

public class ContainerItem : MonoBehaviour
{
    [SerializeField] private Collider[] itemColliders;
    [SerializeField] private WorldSpaceInteractableUI worldUI;

    private void Awake()
    {
        if (itemColliders == null || itemColliders.Length == 0)
        {
            itemColliders = GetComponentsInChildren<Collider>(true);
        }

        if (worldUI == null)
        {
            worldUI = GetComponentInChildren<WorldSpaceInteractableUI>(true);
        }
    }

    public void SetContentState(bool isOpen)
    {
        if (itemColliders != null)
        {
            for (int i = 0; i < itemColliders.Length; i++)
            {
                if (itemColliders[i] != null)
                {
                    itemColliders[i].enabled = isOpen;
                }
            }
        }

        if (worldUI != null)
        {
            if (!isOpen)
            {
                worldUI.HidePrompt();
            }

            // Disabling the entire Canvas / UI GameObject directly
            worldUI.gameObject.SetActive(isOpen);
        }
    }
}