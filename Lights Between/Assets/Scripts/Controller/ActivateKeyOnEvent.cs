using UnityEngine;

public class ActivateKeyOnEvent : MonoBehaviour
{
    [Header("Event Channel")]
    [SerializeField] private GameEventSO onEnterGarageEvent;

    [Header("Target Key Object")]
    [SerializeField] private GameObject garageKeyObject;

    private void Awake()
    {
        if (garageKeyObject == null)
        {
            garageKeyObject = gameObject;
        }

        if (garageKeyObject != null)
        {
            garageKeyObject.SetActive(false);
        }
    }

    private void OnEnable()
    {
        if (onEnterGarageEvent != null)
        {
            onEnterGarageEvent.RegisterListener(HandleEnterGarage);
        }
    }

    private void OnDisable()
    {
        if (onEnterGarageEvent != null)
        {
            onEnterGarageEvent.UnregisterListener(HandleEnterGarage);
        }
    }

    private void HandleEnterGarage()
    {
        if (garageKeyObject != null)
        {
            garageKeyObject.SetActive(true);
        }
    }
}