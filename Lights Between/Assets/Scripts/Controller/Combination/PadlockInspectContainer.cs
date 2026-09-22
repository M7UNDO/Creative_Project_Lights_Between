using UnityEngine;

public class PadlockInspectContainer : MonoBehaviour
{
    [SerializeField] private CombinationPadlock.PadlockDial[] inspectDials;

    public CombinationPadlock.PadlockDial[] GetDials()
    {
        return inspectDials;
    }
}