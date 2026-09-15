using UnityEngine;
using System.Collections.Generic;

public class SwitchLights : MonoBehaviour
{
    public Texture2D[] darkLightmapDir, darkLightmapColour;
    public Texture2D[] brightLightmapDir, brightLightmapColour;

    private LightmapData[] darkLightmap, brightLightmap;

    private void Start()
    {
        List<LightmapData> dlightmap = new List<LightmapData>();

        for(int i = 0; i< darkLightmapDir.Length; i++)
        {
            LightmapData lmdata = new LightmapData();

            lmdata.lightmapDir = darkLightmapDir[i];
            lmdata.lightmapColor = darkLightmapColour[i];

            dlightmap.Add(lmdata);
        }

        darkLightmap = dlightmap.ToArray();


        List<LightmapData> blightmap = new List<LightmapData>();

        for(int i = 0; i< brightLightmapDir.Length; i++)
        {
            LightmapData lmdata = new LightmapData();

            lmdata.lightmapDir = brightLightmapDir[i];
            lmdata.lightmapColor = brightLightmapColour[i];

            blightmap.Add(lmdata);
        }

        brightLightmap = blightmap.ToArray();

    }

    private void OnLightSwitchedOff()
    {
        LightmapSettings.lightmaps = darkLightmap;
    }


    private void OnLightSwitchedOn()
    {
        LightmapSettings.lightmaps = brightLightmap;
    }
}


