using UnityEngine;

public class CreatureLightStatus : MonoBehaviour
{
    public bool isInLight;
    private int lightCount;

    public void EnterLight()
    {
        lightCount++;
        isInLight = true;
    }

    public void ExitLight()
    {
        lightCount--;

        if (lightCount <= 0)
        {
            lightCount = 0;
            isInLight = false;
        }
    }

    public void ResetLightStatus()
    {
        lightCount = 0;
        isInLight = false;
    }
}