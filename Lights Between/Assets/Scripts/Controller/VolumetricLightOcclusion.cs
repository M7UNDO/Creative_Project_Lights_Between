using UnityEngine;

public class VolumetricLightOcclusionManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private VolumetricAdditionalLight[] volumetricLights;

    [Header("Occlusion Settings")]
    [SerializeField] private LayerMask occlusionLayer;
    [SerializeField] private float checkInterval = 0.1f;

    private float checkTimer;

    private void Awake()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;
    }

    private void Update()
    {
        checkTimer -= Time.deltaTime;

        if (checkTimer > 0f)
            return;

        checkTimer = checkInterval;

        CheckLightOcclusion();
    }

    private void CheckLightOcclusion()
    {
        if (targetCamera == null)
            return;

        Vector3 origin = targetCamera.transform.position;

        foreach (VolumetricAdditionalLight volumetricLight in volumetricLights)
        {
            if (volumetricLight == null)
                continue;

            Vector3 targetPosition = volumetricLight.transform.position;
            Vector3 direction = (targetPosition - origin).normalized;
            float distance = Vector3.Distance(origin, targetPosition);

            bool isOccluded = Physics.Raycast(
                origin,
                direction,
                distance,
                occlusionLayer,
                QueryTriggerInteraction.Ignore
            );

            volumetricLight.enabled = !isOccluded;
        }
    }
}