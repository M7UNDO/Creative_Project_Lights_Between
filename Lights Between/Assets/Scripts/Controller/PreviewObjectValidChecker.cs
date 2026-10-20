using System.Collections.Generic;
using UnityEngine;

public class PreviewObjectValidChecker : MonoBehaviour
{
    [SerializeField] private LayerMask invalidLayers;
    public bool IsValid { get; private set; } = true;

    private readonly HashSet<Collider> _collidingObjects = new HashSet<Collider>();

    private void OnEnable()
    {
        _collidingObjects.Clear();
        IsValid = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.isTrigger) return;

        if (((1 << other.gameObject.layer) & invalidLayers) != 0)
        {
            _collidingObjects.Add(other);
            IsValid = false;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.isTrigger) return;

        if (((1 << other.gameObject.layer) & invalidLayers) != 0)
        {
            _collidingObjects.Remove(other);
            CleanNullReferences();
            IsValid = _collidingObjects.Count <= 0;
        }
    }

    private void CleanNullReferences()
    {
        _collidingObjects.RemoveWhere(col => col == null || !col.enabled || !col.gameObject.activeInHierarchy);
    }
}