using UnityEngine;

public class LightCullManager : MonoBehaviour
{
    [SerializeField] private float maxVisibleDistance = 25f;
    private Light[] allLights;
    private Transform playerTransform;

    private void Start()
    {
        allLights = FindObjectsByType<Light>(FindObjectsSortMode.None);
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) playerTransform = player.transform;
    }

    private void Update()
    {
        if (playerTransform == null) return;

        float distanceThresholdSq = maxVisibleDistance * maxVisibleDistance;
        Vector3 playerPos = playerTransform.position;

        foreach (Light light in allLights)
        {
            if (light == null) continue;

            float distSq = (light.transform.position - playerPos).sqrMagnitude;
            light.renderMode = distSq > distanceThresholdSq ? LightRenderMode.ForceVertex : LightRenderMode.ForcePixel;
        }
    }
}