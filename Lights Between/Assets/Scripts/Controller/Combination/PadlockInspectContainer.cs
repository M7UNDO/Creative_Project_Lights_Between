using UnityEngine;

public class PadlockInspectContainer : MonoBehaviour
{
    [SerializeField] private CombinationPadlock.PadlockDial[] inspectDials;
    [SerializeField] private float[] dialXPositions = new float[4];

    public CombinationPadlock.PadlockDial[] GetDials()
    {
        return inspectDials;
    }

    public float[] GetDialXPositions()
    {
        return dialXPositions;
    }
}