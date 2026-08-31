using UnityEngine;

public class PlayerLightStatus : MonoBehaviour
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
}