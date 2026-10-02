using UnityEngine;

public class PadlockInspectContainer : MonoBehaviour
{
    [SerializeField] private Outline containerOutline;
    [SerializeField] private CombinationPadlock.PadlockDial[] inspectDials;
    [SerializeField] private float[] dialXPositions = new float[4];

    public Outline GetOutline()
    {
        return containerOutline;
    }

    public CombinationPadlock.PadlockDial[] GetDials()
    {
        return inspectDials;
    }

    public float[] GetDialXPositions()
    {
        return dialXPositions;
    }
}