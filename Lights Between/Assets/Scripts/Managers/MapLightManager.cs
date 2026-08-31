using System.Collections.Generic;
using UnityEngine;

public class MapLightManager : MonoBehaviour
{
    public static MapLightManager Instance { get; private set; }

    [SerializeField] private List<LightSwitch> allLightSwitches = new List<LightSwitch>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        PopulateLightSwitches();
    }

    public void PopulateLightSwitches()
    {
        allLightSwitches.Clear();
        LightSwitch[] foundSwitches = Object.FindObjectsByType<LightSwitch>(FindObjectsSortMode.None);
        allLightSwitches.AddRange(foundSwitches);
    }

    public void RegisterLightSwitch(LightSwitch lightSwitch)
    {
        if (lightSwitch != null && !allLightSwitches.Contains(lightSwitch))
        {
            allLightSwitches.Add(lightSwitch);
        }
    }

    public void UnregisterLightSwitch(LightSwitch lightSwitch)
    {
        if (lightSwitch != null && allLightSwitches.Contains(lightSwitch))
        {
            allLightSwitches.Remove(lightSwitch);
        }
    }

    public int TotalLightSwitches => allLightSwitches.Count;

    public int ActiveLightSwitchesCount
    {
        get
        {
            int activeCount = 0;
            for (int i = 0; i < allLightSwitches.Count; i++)
            {
                if (allLightSwitches[i] != null && allLightSwitches[i].IsLightActive)
                {
                    activeCount++;
                }
            }
            return activeCount;
        }
    }

    public float DarknessPercentage
    {
        get
        {
            if (TotalLightSwitches == 0) return 1f;
            return 1f - ((float)ActiveLightSwitchesCount / TotalLightSwitches);
        }
    }

    public LightSwitch GetRandomActiveLightSwitch()
    {
        List<LightSwitch> activeSwitches = new List<LightSwitch>();
        for (int i = 0; i < allLightSwitches.Count; i++)
        {
            if (allLightSwitches[i] != null && allLightSwitches[i].IsLightActive)
            {
                activeSwitches.Add(allLightSwitches[i]);
            }
        }

        if (activeSwitches.Count == 0) return null;
        return activeSwitches[Random.Range(0, activeSwitches.Count)];
    }
}